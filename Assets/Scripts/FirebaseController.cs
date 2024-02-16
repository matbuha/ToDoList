using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using System;
using Firebase;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;

public class FirebaseController : MonoBehaviour {
    // Reference to Firestore database
    FirebaseFirestore db;

    // Reference to PageManager script for UI navigation
    public PageManager pageManager;

    // Current Firebase user
    private FirebaseUser user;

    // Firebase Authentication instance
    private FirebaseAuth auth;

    // UI elements for various pages and input fields
    public GameObject loginPage, createUserPage, mainPage, forgotPassPage, notificationPanel;
    public TMP_InputField loginEmail, loginPassword, signupEmail, signupPassword,signupConfirmPassword, signupUserName,forgetPassEmail;
    public TMP_Text errorTitleText, errorMessage, profileUserName_Text, profileUserEmail_Text, profileUserId_Text;

    // Flag to check if a user is signed in
    bool isSignIn = false;

    void Start() {
        OpenLoginPage();
        // Check and fix Firebase dependencies at the start
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                // Firebase is ready to use
                InitializeFirebase();
            } else {
                // Handle dependency resolution failure
                Debug.LogError(string.Format("Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            }
        });

        // Initialize Firestore database reference
        if (pageManager == null) {
            pageManager = FindObjectOfType<PageManager>();
        }
        db = FirebaseFirestore.DefaultInstance;
    }

    // Navigation methods to open different pages
    public void OpenLoginPage() {
        pageManager.ShowPage("LoginPage");
    }

    public void OpenMainPage() {
        pageManager.ShowPage("MainPage");
    }

    // Method to log in the user with email and password
    public void LogInUser() {
        if (string.IsNullOrEmpty(loginEmail.text) && string.IsNullOrEmpty(loginPassword.text)) {
            // Show error if fields are empty
            showNotifactionMessage("Error", "Please Fill All Necessary Fields");
        } else {
            // Attempt to sign in the user
            SignInUser(loginEmail.text, loginPassword.text);
        }
    }

    // Method to sign up a new user
    public void SignUpUser() {
        if (string.IsNullOrEmpty(signupEmail.text) && string.IsNullOrEmpty(signupPassword.text)
            && string.IsNullOrEmpty(signupConfirmPassword.text) && string.IsNullOrEmpty(signupUserName.text)) {
            // Show error if fields are empty
            showNotifactionMessage("Error", "Please Fill All Necessary Fields");
        } else {
            // Attempt to create a new user
            CreateUser(signupEmail.text, signupPassword.text, signupUserName.text);
        }
    }

    // Method for password reset
    public void forgetPass() {
        if (string.IsNullOrEmpty(forgetPassEmail.text)) {
            // Show error if field is empty
            showNotifactionMessage("Error", "Please Fill All Necessary Fields");
        } else {
            // Attempt to send a password reset email
            forgetPasswordSubmit(forgetPassEmail.text);
        }
    }

    // Show a notification message
    private void showNotifactionMessage(string title, string message) {
        errorTitleText.text = title;
        errorMessage.text = message;
        notificationPanel.SetActive(true);
    }

    // Close the notification panel
    public void closeNotifactionMessage() {
        errorTitleText.text = "";
        errorMessage.text = "";
        notificationPanel.SetActive(false);
    }

    // Method to log out the current user
    public void Logout() {
        auth.SignOut();
        profileUserId_Text.text = "";
        profileUserEmail_Text.text = "";
        profileUserName_Text.text = "";

        // Clear UI and data for checklist and profile picture
        var checklistManager = FindObjectOfType<ChecklistManager>();
        if (checklistManager != null) {
            checklistManager.ClearUI();
            checklistManager.ClearUserData();
        }

        var profilePictureUploader = FindObjectOfType<ProfilePictureUploader>();
        if (profilePictureUploader != null) {
            profilePictureUploader.ClearProfilePicture();
            // profilePictureUploader.StopListeningForUserUpdates();
        }
        OpenLoginPage();
    }

    // Method to create a new user with email and password
    void CreateUser(string email, string password, string UserName) {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled) {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }

            HandleFirebaseException(task);
            
            if (task.IsCompleted) {
                // Successfully created Firebase user
                AuthResult result = task.Result;
                Debug.LogFormat("Firebase user created successfully: {0} ({1})", result.User.DisplayName, result.User.UserId);
                UpdateUserProfile(UserName);
                OpenLoginPage();
            }
        });
    }

    // Method to sign in an existing user
    void SignInUser(string email, string password) {
        // Clear existing profile picture and checklist data before signing in
        FindObjectOfType<ProfilePictureUploader>()?.ClearProfilePicture();
        FindObjectOfType<ChecklistManager>()?.ClearUserData();

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCanceled) {
                Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                ShowErrorMessage("Login was canceled.");
                return;
            }

            if (!HandleFirebaseException(task)) {
                // Handle any Firebase exceptions
                return;
            }

            if (task.IsCompleted) {
                // Successfully signed in
                AuthResult newUser = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.User.DisplayName, newUser.User.UserId);
                profileUserId_Text.text = newUser.User.UserId;
                profileUserName_Text.text = newUser.User.DisplayName;
                profileUserEmail_Text.text = newUser.User.Email;

                // Retrieve and use user data from Firestore
                DocumentReference docRef = db.Collection("users").Document(newUser.User.UserId);
                docRef.GetSnapshotAsync().ContinueWithOnMainThread(snapshotTask => {
                    if (snapshotTask.IsFaulted) {
                        Debug.LogError("Snapshot task is faulted: " + snapshotTask.Exception);
                        ShowErrorMessage("Error retrieving user data.");
                        return;
                    }

                    if (!snapshotTask.Result.Exists) {
                        // Create a new document for the user if it doesn't exist
                        CreateNewUserDocument(newUser.User.UserId);
                    } else {
                        // Use user data from the snapshot
                        DocumentSnapshot snapshot = snapshotTask.Result;
                    }

                    // Notify ChecklistManager and ProfilePictureUploader about the user change
                    FindObjectOfType<ChecklistManager>()?.SetUser(newUser.User.UserId);
                    FindObjectOfType<ProfilePictureUploader>()?.SetUser(newUser.User.UserId);

                    OpenMainPage(); // Navigate to the main page
                });
            }
        });
    }

    // Create a new Firestore document for the new user
    private void CreateNewUserDocument(string userId) {
        DocumentReference docRef = db.Collection("users").Document(userId);
        Dictionary<string, object> newUserDoc = new Dictionary<string, object> { { "initialized", true } };
        docRef.SetAsync(newUserDoc).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) {
                Debug.LogError("Error creating new user document: " + task.Exception);
            } else {
                Debug.Log("New user document created successfully.");
            }
        });
    }

    // Initialize Firebase Authentication and set up the state changed listener
    void InitializeFirebase() {
        Debug.Log("Setting up Firebase Auth");
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    // Handle Firebase Authentication state changes
    void AuthStateChanged(object sender, EventArgs eventArgs) {
        if (auth.CurrentUser != user) {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null && auth.CurrentUser.IsValid();
            if (!signedIn && user != null) {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn) {
                Debug.Log("Signed in " + user.UserId);
                isSignIn = true;
            }
        }
    }

    // Clean up the auth state changed listener when the object is destroyed
    void OnDestroy() {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    // Update the user's profile with a display name and a placeholder photo URL
    void UpdateUserProfile(string UserName) {
        FirebaseUser user = auth.CurrentUser;
        if (user != null) {
            UserProfile profile = new UserProfile {
                DisplayName = UserName,
                PhotoUrl = new Uri("https://via.placeholder.com/150"),
            };
            user.UpdateUserProfileAsync(profile).ContinueWith(task => {
                if (task.IsCanceled) {
                    Debug.LogError("UpdateUserProfileAsync was canceled.");
                    return;
                }
                if (task.IsFaulted) {
                    Debug.LogError("UpdateUserProfileAsync encountered an error: " + task.Exception);
                    return;
                }

                Debug.Log("User profile updated successfully.");
                showNotifactionMessage("Alert", "Account Successfully Created");
            });
        }
    }

    bool isSigned = false;

    // Method to show the main page if the user is signed in
    void Update() {
        if (isSignIn) {
            if (!isSigned) {
                isSigned = true;
                profileUserId_Text.text = user.UserId;
                profileUserName_Text.text = user.DisplayName;
                profileUserEmail_Text.text = user.Email;
                OpenMainPage();
            }
        }
    }

    // Handle Firebase exceptions and show appropriate error messages
    private bool HandleFirebaseException(Task task) {
        if (task.IsFaulted) {
            foreach (Exception exception in task.Exception.Flatten().InnerExceptions) {
                FirebaseException firebaseEx = exception as FirebaseException;
                if (firebaseEx != null) {
                    var errorCode = (AuthError)firebaseEx.ErrorCode;
                    string message = GetErrorMessage(errorCode);
                    ShowErrorMessage(message);
                    return false;
                }
            }
        }
        return true;
    }

    // Show error messages based on Firebase authentication error codes
    private void ShowErrorMessage(string message) {
        Debug.LogError(message);
        errorMessage.text = message;
        notificationPanel.SetActive(true);
    }

    private static string GetErrorMessage(AuthError errorCode) {
        // Map AuthError codes to error messages
        var message = "";
        switch (errorCode) {
            case AuthError.AccountExistsWithDifferentCredentials:
                message = "The account already exists with different credentials";
                break;
            case AuthError.MissingPassword:
                message = "Password is needed";
                break;
            case AuthError.WeakPassword:
                message = "The password is weak";
                break;
            case AuthError.WrongPassword:
                message = "The password is incorrect";
                break;
            case AuthError.EmailAlreadyInUse:
                message = "The account with that email already exists";
                break;
            case AuthError.InvalidEmail:
                message = "invalid email";
                break;
            case AuthError.MissingEmail:
                message = "Email is needed";
                break;
            default:
                message = "An error occurred";
                break;
        }
        return message;
    }

    // Method to send a password reset email
    void forgetPasswordSubmit(string forgetPasswordEmail) {
        auth.SendPasswordResetEmailAsync(forgetPassEmail.text).ContinueWithOnMainThread(task => {
            if (task.IsCanceled) {
                Debug.LogError("SendPasswordResetEmailAsync was canceled.");
                return;
            }

            HandleFirebaseException(task);

            if (task.IsCompleted) {
                showNotifactionMessage("Alert", "Check Your Email For Further Instructions");
            }
        });
    }

    // Save user credentials locally on Android using a custom SharedPreferencesManager class
    public void SaveCredentials(string email, string password) {
        if (Application.platform == RuntimePlatform.Android) {
            using (var javaClass = new AndroidJavaClass("com.yourcompany.plugin.SharedPreferencesManager")) {
                using (var unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                    var context = unityActivity.GetStatic<AndroidJavaObject>("currentActivity");
                    javaClass.CallStatic("setRememberMe", context, email, password);
                }
            }
        }
    }

    // Load saved user credentials on Android using the custom SharedPreferencesManager class
    public void LoadCredentials() {
        if (Application.platform == RuntimePlatform.Android) {
            string email = "", password = "";
            using (var javaClass = new AndroidJavaClass("com.arielbz.ChubbyChampsANDROID.plugin.SharedPreferencesManager")) {
                using (var unityActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                    var context = unityActivity.GetStatic<AndroidJavaObject>("currentActivity");
                    email = javaClass.CallStatic<string>("getUserEmail", context);
                    password = javaClass.CallStatic<string>("getUserPassword", context);
                }
            }
        }
    }

    // Method triggered by the refresh button click to update user data and profile picture
    public void OnRefreshButtonClicked() {
        var checklistManager = FindObjectOfType<ChecklistManager>();
        if (checklistManager != null) {
            checklistManager.LoadChecklistDataFromFirestore(); // Refresh checklist data
        }

        var profilePictureUploader = FindObjectOfType<ProfilePictureUploader>();
        if (profilePictureUploader != null) {
            profilePictureUploader.LoadProfilePicture(); // Refresh profile picture
        }
    }
}

using System.Collections;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Storage;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using SimpleFileBrowser;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class ProfilePictureUploader : MonoBehaviour {

    public RawImage profileImageDisplay; // UI component for displaying the profile picture.
    private FirebaseStorage storage; // Reference to Firebase Storage to upload/download images.
    private string userId; // The unique identifier of the current user.
    private string currentProfilePicturePath; // Store the current profile picture path for deletion.

    // Called when the script instance is being loaded.
    void Start() {
        // Get a reference to the default Firebase Storage instance.
        storage = FirebaseStorage.DefaultInstance;
        // Initialize the user ID from the current Firebase Auth user, if available.
        SetUser(FirebaseAuth.DefaultInstance.CurrentUser?.UserId);
    }

    // Sets the current user for this profile picture uploader instance.
    public void SetUser(string userId) {
        // Set the class-wide userId variable.
        this.userId = userId;
        // If a userId is provided, initiate loading of the profile picture from Firestore.
        if (!string.IsNullOrEmpty(userId)) {
            LoadProfilePicture();
        }
    }

    // Loads the profile picture from Firebase Storage if the path is available in Firestore.
    public void LoadProfilePicture() {
        // Check to ensure we have a valid userId.
        if (string.IsNullOrEmpty(userId)) return;

        // Access Firestore to retrieve the document for the current user.
        var userDocRef = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);
        userDocRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogError("Failed to fetch user document: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists && snapshot.ContainsField("profilePicturePath")) {
                currentProfilePicturePath = snapshot.GetValue<string>("profilePicturePath");
                StorageReference imageRef = storage.GetReference(currentProfilePicturePath);
                imageRef.GetDownloadUrlAsync().ContinueWithOnMainThread(downloadTask => {
                    if (!downloadTask.IsFaulted && !downloadTask.IsCanceled) {
                        StartCoroutine(DownloadImage(downloadTask.Result.ToString()));
                    } else {
                        Debug.LogError("Failed to fetch profile picture URL: " + downloadTask.Exception);
                    }
                });
            } else {
                Debug.Log("Profile picture path not found.");
            }
        });
    }

    // Coroutine to download and display the image from a URL.
    IEnumerator DownloadImage(string url) {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
            Debug.LogError("Error downloading image: " + request.error);
        } else {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            profileImageDisplay.texture = texture;
        }
    }

    // Opens the file browser for the user to select an image to upload.
    public void OpenFileBrowser() {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    // Coroutine to show the load dialog and upload the selected image.
    IEnumerator ShowLoadDialogCoroutine() {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Image", "Load");
        if (FileBrowser.Success) {
            // Delete the old picture from storage if it exists.
            if (!string.IsNullOrEmpty(currentProfilePicturePath)) {
                DeleteOldProfilePicture(currentProfilePicturePath);
            }
            UploadProfilePicture(FileBrowser.Result[0]);
        }
    }

    // Deletes the old profile picture from Firebase Storage.
    private void DeleteOldProfilePicture(string path) {
        StorageReference oldImageRef = storage.GetReference(path);
        oldImageRef.DeleteAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogError("Error deleting old profile picture: " + task.Exception);
            } else {
                Debug.Log("Old profile picture deleted successfully.");
            }
        });
    }

    // Uploads the selected image to Firebase Storage and updates Firestore with the new image path.
    private void UploadProfilePicture(string selectedFilePath) {
        if (string.IsNullOrEmpty(userId)) return;

        // Generate a unique file name for the new profile picture.
        string uniqueFileName = $"profilePic_{DateTime.Now.Ticks}.png";
        string newPath = $"users/{userId}/{uniqueFileName}";

        // Get a reference to the new image location in Firebase Storage.
        StorageReference newImageRef = storage.GetReference(newPath);

        // Upload the file to Firebase Storage.
        newImageRef.PutFileAsync(selectedFilePath).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogError("Profile picture upload failed: " + task.Exception);
            } else {
                // Update Firestore with the new picture's path
                SaveProfilePicturePathToFirestore(userId, newPath);
                Debug.Log("Profile picture uploaded successfully.");
                currentProfilePicturePath = newPath; // Update the stored path with the new image path.
                LoadProfilePicture(); // Reload the profile image to reflect the new upload.
            }
        });
    }

    // Updates Firestore with the path to the newly uploaded profile picture.
    private void SaveProfilePicturePathToFirestore(string userId, string path) {
        var userDocRef = FirebaseFirestore.DefaultInstance.Collection("users").Document(userId);
        userDocRef.UpdateAsync("profilePicturePath", path).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) {
                Debug.LogError("Failed to save profile picture path: " + task.Exception);
            } else {
                Debug.Log("Profile picture path saved successfully.");
            }
        });
    }

    // Clears the currently displayed profile picture.
    public void ClearProfilePicture() {
        profileImageDisplay.texture = null;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

// Manages checklist items within the app, including creating, displaying, and deleting them
public class ChecklistManager : MonoBehaviour {

    
    private FirebaseAuth auth; // Firebase Authentication instance for user management
    private FirebaseUser user; // Represents the current Firebase user

    public PageManager pageManager; // Reference to manage navigation between different UI pages
    public Transform content; // Parent transform for dynamically created checklist items
    public GameObject addPanel; // UI panel for adding new checklist items
    public Button addButton, createButton; // Buttons for adding items and creating new items
    public GameObject checklistItemPrefab; // Prefab for instantiating new checklist items

    private List<ChecklistObject> checklistObjects = new List<ChecklistObject>(); // List to keep track of all checklist items
    public TMP_InputField itemNameInputField, itemTypeInputField; // Input fields for item name and type

    FirebaseFirestore db; // Firestore database instance

    // Represents a single checklist item
    [Serializable]
    public class ChecklistItem {
        public string id; // Unique identifier for the checklist item
        public string objName;
        public string type;
        public int index;

        // Constructor to initialize a new checklist item
        public ChecklistItem(string id,string name, string content, int index) {
            this.id = id;
            this.objName = name;
            this.type = content;
            this.index = index;
        }

        // Converts the checklist item to a dictionary for Firestore storage
        public Dictionary<string, object> ToDictionary() {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["id"] = id; // Include ID in the dictionary
            dictionary["objName"] = objName;
            dictionary["type"] = type;
            dictionary["index"] = index;
            return dictionary;
        }

        // Creates a ChecklistItem instance from a Firestore document
        public static ChecklistItem FromDictionary(Dictionary<string, object> dictionary) {
            string id = dictionary["id"] as string; // Retrieve ID from the dictionary
            string name = dictionary["objName"] as string;
            string content = dictionary["type"] as string;
            int index = Convert.ToInt32(dictionary["index"]);
            return new ChecklistItem(id, name, content, index);
        }
    }

    // Initialize Firestore and Firebase on start
    void Start() {
        db = FirebaseFirestore.DefaultInstance;
        // Check and fix Firebase dependencies
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available) {
                // Firebase and Firestore are available
                InitializeFirebase();
                InitializeFirestore();
            } else {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
                // Handle the unavailability of Firebase and Firestore
            }
        });

        // Optionally, get the PageManager if it's not set
        if (pageManager == null) {
            pageManager = FindObjectOfType<PageManager>();
        }
    }

    // Sets up Firebase Authentication
    void InitializeFirebase() {
        Debug.Log("Setting up Firebase Auth");
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    // Sets up Firestore
    void InitializeFirestore() {
        Debug.Log("Setting up Firestore");
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        // Already initialized db at the start, additional setup can be added here
    }

    // Handles changes in the Firebase auth state (e.g., user logs in or out)
    void AuthStateChanged(object sender, EventArgs eventArgs) {
        if (auth.CurrentUser != user) {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null && auth.CurrentUser.IsValid();
            if (!signedIn && user != null) {
                Debug.Log("Signed out " + user.UserId);
                // Perform actions needed after user signs out
            }
            user = auth.CurrentUser;
            if (signedIn) {
                Debug.Log("Signed in " + user.UserId);
                // Perform actions needed after user signs in
            }
        }
    }

    // Switches UI modes between adding items and viewing the checklist
    public void SwitchMode(int mode) {
        switch (mode) {
            // Mode 0: Regular checklist view
            case 0:
                addPanel.SetActive(false); // Hide the addPanel
                addButton.gameObject.SetActive(true);
                break;
            
            // Mode 1: Adding a new checklist item
            case 1:
                addPanel.SetActive(true); // Show the addPanel
                addButton.gameObject.SetActive(false);
                break;

        }
    }

    // Loads checklist data for the current user from Firestore
    public void LoadChecklistData() {
        // Ensure there's a logged-in user
        if (auth.CurrentUser == null) {
            Debug.LogError("No user logged in.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        CollectionReference checklistCollection = db.Collection("users");

        checklistCollection.Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogError("Error fetching checklist data: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists) {
                Dictionary<string, object> checklistData = snapshot.ToDictionary();
            } else {
                Debug.Log("No checklist data found for user: " + userId);
            }
        });
    }

    // Triggered when the user clicks to create a new checklist item
    public void OnCreateButtonClicked() {
        string itemId = Guid.NewGuid().ToString(); // Generate a unique ID for the new item
        string itemName = itemNameInputField.text; // Get name from input field
        string itemType = itemTypeInputField.text; // Get type from input field
        int itemIndex = checklistObjects.Count; // Index can be the count of existing items

        CreateChecklistItem(itemId, itemName, itemType, itemIndex);
        SwitchMode(0);
    }

    // Creates a new checklist item and adds it to the UI and Firestore
    public void CreateChecklistItem(string itemId, string name, string type, int loadIndex = 0, bool loading = false) {
        if (!loading) {
            // Create a new checklist item locally
            GameObject item = Instantiate(checklistItemPrefab);
            item.transform.SetParent(content, false); // Set 'false' for worldPositionStays to maintain local orientation and scale

            ChecklistObject itemObject = item.GetComponent<ChecklistObject>();
            int index = checklistObjects.Count;
            itemObject.SetObjectInfo(itemId, name, type, index);
            checklistObjects.Add(itemObject);

            // Save all checklist items to Firestore
            SaveChecklistDataToFirestore();
        } else {
            // This branch is for loading existing items from Firestore
            // Create the checklist item from loaded data
            GameObject item = Instantiate(checklistItemPrefab);
            item.transform.SetParent(content, false); // Set 'false' for worldPositionStays to maintain local orientation and scale
            ChecklistObject itemObject = item.GetComponent<ChecklistObject>();
            itemObject.SetObjectInfo(itemId, name, type, loadIndex);
            checklistObjects.Add(itemObject);
        }
    }

    // Saves all current checklist items to Firestore under the current user's document
    public void SaveChecklistDataToFirestore() {
        if (auth.CurrentUser == null) {
            Debug.LogError("No user logged in. Cannot save checklist data.");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        DocumentReference docRef = db.Collection("users").Document(userId).Collection("checklists").Document("data");

        List<Dictionary<string, object>> itemsData = new List<Dictionary<string, object>>();
        foreach (var checklistObj in checklistObjects) {
            Dictionary<string, object> itemDict = new Dictionary<string, object> {
                {"id", checklistObj.id },
                { "objName", checklistObj.objName },
                { "type", checklistObj.type },
                { "index", checklistObj.index }
            };
            itemsData.Add(itemDict);
        }

        Dictionary<string, object> update = new Dictionary<string, object> {
            { "items", itemsData }
        };

        docRef.SetAsync(update).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) {
                Debug.LogError("Error updating Firestore: " + task.Exception);
            } else {
                Debug.Log("Checklist data updated successfully.");
            }
        });
    }

    // Destroys a checklist item GameObject after a delay
    IEnumerator DestroyAfterDelay(GameObject item, float delay) {
    // Wait for the specified delay
    yield return new WaitForSeconds(delay);

    // Destroy the item
    Destroy(item);
    }

    float timeToDestroy = 0.5f;

    // Called when a checklist item's status changes (e.g., item checked off)
    public void CheckItem(ChecklistObject item) {
        // Remove the item from the list of checklist items
        checklistObjects.Remove(item);

        // Update the Firestore database
        UpdateChecklistDataInFirestore();

        // Start coroutine to destroy the item after a delay
        StartCoroutine(DestroyAfterDelay(item.gameObject, timeToDestroy));
    }

    // Updates Firestore with the current list of checklist items
    void UpdateChecklistDataInFirestore() {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId).Collection("checklists").Document("data");

        // Prepare the data to be saved
        Dictionary<string, object> data = new Dictionary<string, object>();
        List<Dictionary<string, object>> itemsData = new List<Dictionary<string, object>>();
        foreach (var checklistItem in checklistObjects) {
            itemsData.Add(new Dictionary<string, object>{
                {"id", checklistItem.id },
                { "objName", checklistItem.objName },
                { "type", checklistItem.type },
                { "index", checklistItem.index }
            });
        }
        data["items"] = itemsData;

        // Update the Firestore document
        docRef.SetAsync(data).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) {
                Debug.LogError("Error updating Firestore: " + task.Exception);
            } else {
                Debug.Log("Checklist data updated successfully.");
            }
        });
    }

    // Loads checklist data from Firestore and updates the UI
    public void LoadChecklistDataFromFirestore() {
        ClearUI(); // Clear existing data
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId).Collection("checklists").Document("data");

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || !task.Result.Exists) {
                Debug.LogWarning("Error fetching Firestore document or document does not exist: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> data = snapshot.ToDictionary();

            if (data.TryGetValue("items", out object itemsObj) && itemsObj is List<object> itemsList) {
                foreach (object itemObj in itemsList) {
                    if (itemObj is Dictionary<string, object> itemDict) {
                        string itemId = itemDict.TryGetValue("id", out object idObj) ? idObj.ToString() : "";
                        string name = itemDict.TryGetValue("objName", out object nameObj) ? nameObj.ToString() : "";
                        string type = itemDict.TryGetValue("type", out object typeObj) ? typeObj.ToString() : "";
                        int index = itemDict.TryGetValue("index", out object indexObj) && int.TryParse(indexObj.ToString(), out int idx) ? idx : 0;

                        // Create the checklist item in the UI
                    CreateChecklistItem(itemId, name, type, index, true);
                }
            }
        }
    });
    }

    // Clears all checklist items from the UI
    public void ClearUI() {
        checklistObjects.Clear();
        foreach (Transform child in content) {
            Destroy(child.gameObject);
        }
    }

    // Sets the current user and loads their checklist data
    public void SetUser(string userId) {
        // Clear existing tasks
        checklistObjects.Clear();
        // Clear UI
        ClearUI();

        // Update the FirebaseUser and Firestore references
        user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null && user.UserId == userId) {
            LoadChecklistDataFromFirestore();
        } else {
            Debug.LogError("User mismatch or null user. Cannot load data for the given userId.");
        }
    }

    // Clears all data related to the current user
    public void ClearUserData() {
        // Clears the list of checklist objects
        checklistObjects.Clear();
    }

}
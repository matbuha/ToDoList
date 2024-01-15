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


public class ChecklistManager : MonoBehaviour {

    
    private FirebaseAuth auth;  // Declare the FirebaseAuth instance
    private FirebaseUser user;  // Declare the FirebaseUser instance

    public PageManager pageManager; // add a reference to the PageManager script
    public Transform content;
    public GameObject addPanel;
    public Button addButton, createButton;
    public GameObject checklistItemPrefab;

    private List<ChecklistObject> checklistObjects = new List<ChecklistObject>();
    private TMP_InputField[] addInputFields;
    public TMP_InputField itemNameInputField; // Drag your item name input field here in the Unity Editor
    public TMP_InputField itemTypeInputField; // Drag your item type input field here in the Unity Editor

    FirebaseFirestore db;

    [System.Serializable]
    public class ChecklistItem {
        public string objName;
        public string type;
        public int index;

        // Constructor to easily create a ChecklistItem
        public ChecklistItem(string name, string content, int index) {
            this.objName = name;
            this.type = content;
            this.index = index;
        }

        // Convert ChecklistItem to a Dictionary for Firestore
        public Dictionary<string, object> ToDictionary() {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["objName"] = objName;
            dictionary["type"] = type;
            dictionary["index"] = index;
            return dictionary;
        }

        // Create a ChecklistItem from a Dictionary (retrieved from Firestore)
        public static ChecklistItem FromDictionary(Dictionary<string, object> dictionary) {
            string name = dictionary["objName"] as string;
            string content = dictionary["type"] as string;
            int index = Convert.ToInt32(dictionary["index"]);
            return new ChecklistItem(name, content, index);
        }
    }

        // Start is called before the first frame update
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

    void InitializeFirebase() {
        Debug.Log("Setting up Firebase Auth");
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void InitializeFirestore() {
        Debug.Log("Setting up Firestore");
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        // Further Firestore initializations (if required)
    }

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

    // Method to switch between different UI modes
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
                // Process and use the checklistData here
                // You need to convert this data back to your ChecklistItem format
                // and populate it in your UI or data structures.
            } else {
                Debug.Log("No checklist data found for user: " + userId);
            }
        });
    }

    public void OnCreateButtonClicked() {
        // Assuming you have input fields for name and type, and you generate the index
        string itemName = itemNameInputField.text; // Get name from input field
        string itemType = itemTypeInputField.text; // Get type from input field
        int itemIndex = checklistObjects.Count; // Index can be the count of existing items

        CreateChecklistItem(itemName, itemType, itemIndex);
        SwitchMode(0);
    }

        // Method to create a new checklist item
    public void CreateChecklistItem(string name, string type, int loadIndex = 0, bool loading = false) {
        if (!loading) {
            // Create a new checklist item locally
            GameObject item = Instantiate(checklistItemPrefab);
            item.transform.SetParent(content, false); // Set 'false' for worldPositionStays to maintain local orientation and scale
            ChecklistObject itemObject = item.GetComponent<ChecklistObject>();
            int index = checklistObjects.Count;
            itemObject.SetObjectInfo(name, type, index);
            checklistObjects.Add(itemObject);

            // Save all checklist items to Firestore
            SaveChecklistDataToFirestore();
        } else {
            // This branch is for loading existing items from Firestore
            // Create the checklist item from loaded data
            GameObject item = Instantiate(checklistItemPrefab);
            item.transform.SetParent(content, false); // Set 'false' for worldPositionStays to maintain local orientation and scale
            ChecklistObject itemObject = item.GetComponent<ChecklistObject>();
            itemObject.SetObjectInfo(name, type, loadIndex);
            checklistObjects.Add(itemObject);
        }
    }


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


    IEnumerator DestroyAfterDelay(GameObject item, float delay) {
    // Wait for the specified delay
    yield return new WaitForSeconds(delay);

    // Destroy the item
    Destroy(item);
    Debug.Log("DestroyAfterDelay worked");
    }

    float timeToDestroy = 0.5f;

    // Method to handle checklist item changes
    public void CheckItem(ChecklistObject item) {
        // Remove the item from the list of checklist items
        Debug.Log("Deleting item: " + item.objName);
        checklistObjects.Remove(item);

        // Update the Firestore database
        UpdateChecklistDataInFirestore();

        // Start coroutine to destroy the item after a delay
        StartCoroutine(DestroyAfterDelay(item.gameObject, timeToDestroy));
        Debug.Log("CheckItem worked");
    }

    void UpdateChecklistDataInFirestore() {
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference docRef = db.Collection("users").Document(user.UserId).Collection("checklists").Document("data");

        // Prepare the data to be saved
        Dictionary<string, object> data = new Dictionary<string, object>();
        List<Dictionary<string, object>> itemsData = new List<Dictionary<string, object>>();
        foreach (var checklistItem in checklistObjects) {
            itemsData.Add(new Dictionary<string, object>{
                { "name", checklistItem.objName },
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

    void LoadChecklistDataFromFirestore() {
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
                        string name = itemDict.TryGetValue("name", out object nameObj) ? nameObj.ToString() : "";
                        string type = itemDict.TryGetValue("type", out object typeObj) ? typeObj.ToString() : "";
                        int index = itemDict.TryGetValue("index", out object indexObj) && int.TryParse(indexObj.ToString(), out int idx) ? idx : 0;
                        // Create the checklist item in the UI
                    CreateChecklistItem(name, type, index, true);
                }
            }
        }
    });
    }

    public void ClearUI() {
        foreach (Transform child in content) {
            Destroy(child.gameObject);
        }
    }

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

    public void ClearUserData() {
        checklistObjects.Clear();
    }

}
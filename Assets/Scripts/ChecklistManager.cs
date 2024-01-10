using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChecklistManager : MonoBehaviour {

    // Public variables for linking Unity objects and UI elements
    public Transform content; // Parent transform for checklist items
    public GameObject addPanel; // Panel for adding new checklist items
    public GameObject openPanel; // Panel for open existing checklist items
    public Button addButton;
    public Button createButton; // Button to create a new checklist item
    public GameObject checklistItemPrefab; // Prefab for checklist items

    string filePath; // Path for saving checklist data

    // A list to keep track of checklist items
    private List<ChecklistObject> checklistObjects = new List<ChecklistObject>();

    // Array for storing input fields from the addPanel
    private TMP_InputField[] addInputFields;

    public class ChecklistItem {
        public string objName;
        public string type;
        public int index;

        public ChecklistItem (string name, string content, int index) {
        objName = name;
        type = content;
        this.index = index;
    }

    }

    // Start is called before the first frame update
    private void Start() {
        // Set the file path for saving checklist data
        filePath = Application.persistentDataPath + "/checklist.txt";

        LoadJSONData();

        // Get all InputField components from addPanel
        addInputFields = addPanel.GetComponentsInChildren<TMP_InputField>();
        
        createButton.onClick.AddListener(delegate {CreateChecklistItem(addInputFields[0].text, addInputFields[1].text); });
        
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

            // Mode 2: Open exist checklist item
            case 2:
                openPanel.SetActive(true); // Show the openPanel
                addButton.gameObject.SetActive(false);
                break;
        }
    }

    // Method to create a new checklist item
    void CreateChecklistItem(string name, string type, int loadIndex = 0, bool loading = false) {
        // Create a new checklist item from the prefab
        GameObject item = Instantiate(checklistItemPrefab);

        // Set the parent of the new item to 'content'
        item.transform.SetParent(content);

        // Get the ChecklistObject component of the new item
        ChecklistObject itemObject = item.GetComponent<ChecklistObject>();

        // Determine the index for the new item

        int index = loadIndex;
        if (!loading) {
            index = checklistObjects.Count;
        }
            
        // Set the details of the new checklist item
        itemObject.SetObjectInfo(name, type, index);

        // Add the new item to the list of checklist items
        checklistObjects.Add(itemObject);

        // Temporarily store the itemObject
        ChecklistObject temp = itemObject;

        // Add a listener to handle changes in the checklist item's toggle state
        itemObject.GetComponent<Toggle>().onValueChanged.AddListener(delegate {CheckItem(temp); });

        if (!loading) {
            SaveJSONData();
            // Switch back to the regular checklist view
            SwitchMode(0);
        }
    }

    IEnumerator DestroyAfterDelay(GameObject item, float delay) {
    // Wait for the specified delay
    yield return new WaitForSeconds(delay);

    // Destroy the item
    Destroy(item);
    }

    float timeToDestroy = 0.5f;

    // Method to handle checklist item changes
    void CheckItem(ChecklistObject item) {
        // Remove the item from the list of checklist items
        checklistObjects.Remove(item);
        SaveJSONData();
        StartCoroutine(DestroyAfterDelay(item.gameObject, timeToDestroy));
    }

    void SaveJSONData() {
        string contents = "";

        for (int i = 0; i < checklistObjects.Count; i++) {
            ChecklistItem temp = new ChecklistItem(checklistObjects[i].objName, checklistObjects[i].type, checklistObjects[i].index);
            contents += JsonUtility.ToJson(temp) + "\n";
        }

        File.WriteAllText(filePath, contents);
    }

    void LoadJSONData () {
        if (File.Exists(filePath)) {
            string contents = File.ReadAllText(filePath);
            string[] splitContents = contents.Split('\n');

            foreach (string content in splitContents) {
                if (content.Trim() != "") {
                    ChecklistItem temp = JsonUtility.FromJson<ChecklistItem>(content);
                    CreateChecklistItem(temp.objName, temp.type, temp.index, true);
                }
                
            }
            
        }else{
            Debug.Log("No file");
        }
        
    }
}
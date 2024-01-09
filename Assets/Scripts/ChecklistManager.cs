using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChecklistManager : MonoBehaviour {

    // Public variables for linking Unity objects and UI elements
    public Transform content; // Parent transform for checklist items
    public GameObject addPanel; // Panel for adding new checklist items
    public Button addButton;
    public Button createButton; // Button to create a new checklist item
    public GameObject checklistItemPrefab; // Prefab for checklist items

    string filePath; // Path for saving checklist data

    // A list to keep track of checklist items
    private List<ChecklistObject> checklistObjects = new List<ChecklistObject>();

    // Array for storing input fields from the addPanel
    private TMP_InputField[] addInputFields;

    // Start is called before the first frame update
    private void Start() {
        // Set the file path for saving checklist data
        filePath = Application.persistentDataPath + "/checklist.txt";

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
        }
    }

    // Method to create a new checklist item
    void CreateChecklistItem(string name, string type) {
        // Create a new checklist item from the prefab
        GameObject item = Instantiate(checklistItemPrefab);

        // Set the parent of the new item to 'content'
        item.transform.SetParent(content);

        // Get the ChecklistObject component of the new item
        ChecklistObject itemObject = item.GetComponent<ChecklistObject>();

        // Determine the index for the new item
        int index = 0;
        if(checklistObjects.Count > 0) {
            index = checklistObjects.Count - 1;
        }
        
        // Set the details of the new checklist item
        itemObject.SetObjectInfo(name, type, index);

        // Add the new item to the list of checklist items
        checklistObjects.Add(itemObject);

        // Temporarily store the itemObject
        ChecklistObject temp = itemObject;

        // Add a listener to handle changes in the checklist item's toggle state
        itemObject.GetComponent<Toggle>().onValueChanged.AddListener(delegate {CheckItem(temp); });

        // Switch back to the regular checklist view
        SwitchMode(0);
    }

    // Method to handle checklist item changes
    void CheckItem(ChecklistObject item) {
        // Remove the item from the list of checklist items
        checklistObjects.Remove(item);
    }
}

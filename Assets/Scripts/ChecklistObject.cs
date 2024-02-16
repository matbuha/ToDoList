using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Represents an individual checklist item within the UI
public class ChecklistObject : MonoBehaviour {

    public string id;
    public string objName; // Title of the task
    public string type; // Content of the task
    public int index; // Index of the item in the list

    private Text titleText; // Text component for the title
    private Text contentText; // Text component for the content

    // Initialize component references
    private void Start() {
        // Retrieves Text components from children to display title and content
        Text[] textComponents = GetComponentsInChildren<Text>();
        if(textComponents.Length >= 2) {
            titleText = textComponents[0]; // First Text component for the title
            contentText = textComponents[1]; // Second Text component for the content
        }

        UpdateItemText(); // Updates the UI text components with the item's data
    }

    // Sets the checklist item's information and updates the UI accordingly
    public void SetObjectInfo(string itemId, string titleTxt, string content, int index) {
        id = itemId;
        objName = titleTxt;
        type = content;
        this.index = index;

        UpdateItemText(); // Reflect changes in the UI
    }

    // Updates the Text components to display the current title and content
    private void UpdateItemText() {
        if (titleText != null && contentText != null) {
            titleText.text = objName; // Set the title
            contentText.text = type; // Set the content
        }
    }

    // Called when the toggle associated with the checklist item changes state
    public void OnToggleChanged(bool isOn) {
        if (isOn) {
            // Finds an instance of ChecklistManager in the scene to handle the change
            ChecklistManager checklistManager = FindObjectOfType<ChecklistManager>();
            if (checklistManager != null) {
                Debug.Log("OnToggleChanged worked");
                checklistManager.CheckItem(this); // 'this' refers to the current ChecklistObject instance
            } else {
                Debug.LogError("ChecklistManager not found in the scene.");
            }
        }
    }
}

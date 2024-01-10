using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChecklistObject : MonoBehaviour {
    public string objName; // Title of the task
    public string type; // Content of the task
    public int index;

    private Text titleText; // Text component for the title
    private Text contentText; // Text component for the content

    private void Start() {
        // Assuming you have two Text children: one for the title and one for the content
        Text[] textComponents = GetComponentsInChildren<Text>();
        if(textComponents.Length >= 2) {
            titleText = textComponents[0]; // First Text component for the title
            contentText = textComponents[1]; // Second Text component for the content
        }

        UpdateItemText();
    }

    public void SetObjectInfo(string name, string content, int index) {
        objName = name;
        type = content;
        this.index = index;

        UpdateItemText();
    }

    private void UpdateItemText() {
        if (titleText != null && contentText != null) {
            titleText.text = objName; // Set the title
            contentText.text = type; // Set the content
        }
    }
}

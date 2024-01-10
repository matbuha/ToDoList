using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser; // Make sure to import the namespace from your file browser plugin

public class ProfilePictureUploader : MonoBehaviour {
    public RawImage profileImageDisplay; // UI element to display the profile picture

    void Start() {
        // Setup File Browser (only once)
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png"));
        FileBrowser.SetDefaultFilter(".jpg");
    }

    // Call this method when the user clicks the upload button
    public void OpenFileBrowser() {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine() {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Image", "Load");

        if (FileBrowser.Success) {
            // Load and apply the image
            string filePath = FileBrowser.Result[0];
            Texture2D texture = new Texture2D(2, 2);
            byte[] fileData = File.ReadAllBytes(filePath);
            texture.LoadImage(fileData);
            profileImageDisplay.texture = texture;
        }
    }
}

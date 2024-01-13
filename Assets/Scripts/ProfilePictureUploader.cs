using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser; // Make sure to import the namespace from your file browser plugin
using Firebase.Auth;

public class ProfilePictureUploader : MonoBehaviour {
    public RawImage profileImageDisplay; // UI element to display the profile picture
    private string userId;
    private string filePath;

    void Start() {
        // Setup File Browser (only once)
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png"));
        FileBrowser.SetDefaultFilter(".jpg");

        // Get the current user's ID
        userId = FirebaseAuth.DefaultInstance.CurrentUser?.UserId;
        filePath = Application.persistentDataPath + "/" + userId + "_profilePic.png";

        // Load the profile picture if it exists
        LoadProfilePicture();

    }

    public void SetUser(string userId) {
    string userFolderPath = Application.persistentDataPath + "/" + userId;
    Directory.CreateDirectory(userFolderPath);  // Create the directory if it doesn't exist

    filePath = userFolderPath + "/" + userId + "_profilePic.png";
    LoadProfilePicture();
    }

    public void LoadProfilePicture() {
        if (File.Exists(filePath)) {
            Texture2D texture = new Texture2D(2, 2);
            byte[] fileData = File.ReadAllBytes(filePath);
            texture.LoadImage(fileData);
            profileImageDisplay.texture = texture;
        }
    }

    // Call this method when the user clicks the upload button
    public void OpenFileBrowser() {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    IEnumerator ShowLoadDialogCoroutine() {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Load Image", "Load");

        if (FileBrowser.Success) {
            string selectedFilePath = FileBrowser.Result[0];
            File.Copy(selectedFilePath, filePath, true);
            LoadProfilePicture();
        }
    }

    // Add a method to clear the profile picture when user logs out
    public void ClearProfilePicture() {
        profileImageDisplay.texture = null;
    }
}

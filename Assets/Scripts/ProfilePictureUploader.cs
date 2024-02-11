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

    void Start() {
        storage = FirebaseStorage.DefaultInstance;
        SetUser(FirebaseAuth.DefaultInstance.CurrentUser?.UserId);
    }

    public void SetUser(string userId) {
        this.userId = userId;
        if (!string.IsNullOrEmpty(userId)) {
            LoadProfilePicture();
        }
    }

    public void LoadProfilePicture() {
        if (string.IsNullOrEmpty(userId)) return;

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

    public void OpenFileBrowser() {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

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

    private void UploadProfilePicture(string selectedFilePath) {
        if (string.IsNullOrEmpty(userId)) return;

        string uniqueFileName = $"profilePic_{DateTime.Now.Ticks}.png";
        string newPath = $"users/{userId}/{uniqueFileName}";

        StorageReference newImageRef = storage.GetReference(newPath);
        newImageRef.PutFileAsync(selectedFilePath).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogError("Profile picture upload failed: " + task.Exception);
            } else {
                // Update Firestore with the new picture's path
                SaveProfilePicturePathToFirestore(userId, newPath);
                Debug.Log("Profile picture uploaded successfully.");
                currentProfilePicturePath = newPath; // Update the current picture path
                LoadProfilePicture(); // Reload the new profile picture
            }
        });
    }

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

    public void ClearProfilePicture() {
        profileImageDisplay.texture = null;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Storage;
using Firebase.Auth;
using Firebase.Extensions;
using SimpleFileBrowser;
using System.Threading.Tasks;
using UnityEngine.Networking;


public class ProfilePictureUploader : MonoBehaviour {

    public RawImage profileImageDisplay;
    private FirebaseStorage storage;
    private string userId;

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

    private void LoadProfilePicture() {
        if (string.IsNullOrEmpty(userId)) return;

        // Construct the path where the image should be stored in Firebase Storage
        string path = $"users/{userId}/profilePic.png";
        StorageReference imageRef = storage.GetReference(path);

        // Fetch the download URL and display the image
        imageRef.GetDownloadUrlAsync().ContinueWithOnMainThread(task => {
            if (!task.IsFaulted && !task.IsCanceled) {
                StartCoroutine(DownloadImage(task.Result.ToString()));
            } else {
                Debug.LogError("Failed to fetch profile picture URL: " + task.Exception);
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
            UploadProfilePicture(FileBrowser.Result[0]);
        }
    }

    private void UploadProfilePicture(string localFilePath) {
        if (string.IsNullOrEmpty(userId)) return;

        // Construct the path where the image should be stored in Firebase Storage
        string path = $"users/{userId}/profilePic.png";
        StorageReference imageRef = storage.GetReference(path);

        // Upload the file to Firebase Storage
        imageRef.PutFileAsync(localFilePath).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) {
                Debug.LogError("Profile picture upload failed: " + task.Exception);
            } else {
                Debug.Log("Profile picture uploaded successfully.");
                LoadProfilePicture(); // Reload the profile picture
            }
        });
    }

    public void ClearProfilePicture() {
        profileImageDisplay.texture = null;
    }
}

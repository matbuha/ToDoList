using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageManager : MonoBehaviour {
    // References to the page panels
    public GameObject mainPage;
    public GameObject profilePage;
    public GameObject loginPage;
    public GameObject createUserPage;
    public GameObject forgotPassPage;

    // Method to show a specific page and hide others
    public void ShowPage(string pageName) {
        // Hide all pages
        mainPage.SetActive(false);
        profilePage.SetActive(false);
        loginPage.SetActive(false);
        createUserPage.SetActive(false);
        forgotPassPage.SetActive(false);

        // Show the requested page
        switch (pageName) {
            case "MainPage":
                mainPage.SetActive(true);
                break;
            case "ProfilePage":
                profilePage.SetActive(true);
                break;
            case "LoginPage":
                loginPage.SetActive(true);
                break;
            case "CreateUserPage":
                createUserPage.SetActive(true);
                break;
            case "ForgotPassPage":
                forgotPassPage.SetActive(true);
                break;
        }
    }
}
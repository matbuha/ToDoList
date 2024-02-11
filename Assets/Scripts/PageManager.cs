using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages the UI pages within the application.
public class PageManager : MonoBehaviour {
    // Public variables to reference the different UI panels/pages within the game.
    public GameObject mainPage, profilePage, loginPage, createUserPage, forgotPassPage;

    // Method to display a specific UI page and hide all others.
    public void ShowPage(string pageName) {
        // Initially hide all pages to ensure that only the requested page is shown.
        mainPage.SetActive(false);
        profilePage.SetActive(false);
        loginPage.SetActive(false);
        createUserPage.SetActive(false);
        forgotPassPage.SetActive(false);

        // Determine which page to show based on the input parameter 'pageName'.
        switch (pageName) {
            case "MainPage":
                mainPage.SetActive(true); // Show the main page.
                break;
            case "ProfilePage":
                profilePage.SetActive(true); // Show the profile page.
                break;
            case "LoginPage":
                loginPage.SetActive(true); // Show the login page.
                break;
            case "CreateUserPage":
                createUserPage.SetActive(true); // Show the user creation page.
                break;
            case "ForgotPassPage":
                forgotPassPage.SetActive(true); // Show the forgot password page.
                break;
        }
    }
}

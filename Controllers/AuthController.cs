﻿using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;
using Stepmeet_BE.Models;
using Google.Cloud.Firestore.V1;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using System.Web;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly FirestoreDb _firestoreDb;
    readonly FirebaseAuthConfig config;
    readonly FirebaseAuthClient client;
    
    public AuthController(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
        config = new FirebaseAuthConfig
        {
            ApiKey = "AIzaSyCz_txhHE4AKenknZcphkhLnpZOavvJH80",
            AuthDomain = "stepmeet-8ed5d.firebaseapp.com",
            Providers = new FirebaseAuthProvider[]
            {
                new EmailProvider()
            }
        };
        client = new FirebaseAuthClient(config);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLogin model)
    {
        try
        {
            var userCredentials = await client.SignInWithEmailAndPasswordAsync(model.Email, model.Password);
            if (userCredentials != null)
            {
                // Fetch user details from Firestore using the user's UID
                var userDoc = _firestoreDb.Collection("users").Document(userCredentials.User.Uid);
                var userSnapshot = await userDoc.GetSnapshotAsync();
                if (userSnapshot.Exists)
                {
                    var userData = userSnapshot.ToDictionary();
                    // Send user details to the frontend
                    return Ok(new { Message = "Login successful", User = userData });
                }
                else
                {
                    return NotFound("User not found");
                }
            }
            else
            {
                return Unauthorized("Incorrect email or password");
            }
        }
        catch (Exception ex)
        {
            // If authentication fails, return an error response
            return Unauthorized(new { Message = "Invalid email or password.", Error = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistration model)
    {
        try
        {
            // Create user in Firebase Authentication
            var auth = FirebaseAuth.DefaultInstance;
            var userRecord = await auth.CreateUserAsync(new UserRecordArgs
            {
                Email = model.Email,
                Password = model.Password,
            });

            // Store user details in Firestore
            var user = new
            {
                firstName= model.FirstName,
                lastName= model.LastName,
                email = model.Email,
                favourites = model.Favorites,
                isPrivate = model.IsPrivate,
                following = model.Following,
                completed = model.Completed,
                dpUrl = model.DPUrl
                // Add more user details as needed
            };

            var userDoc = _firestoreDb.Collection("users").Document(userRecord.Uid);
            await userDoc.SetAsync(user);

            return Ok(new { Message = "User registered successfully", User = user });

        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpDelete("{email}/delete")]
    public async Task<IActionResult> DeleteUserByEmail(string email)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .GetSnapshotAsync();

            if (querySnapshot.Count > 0)
            {
                // Get the reference to the first user document with the matching email
                DocumentSnapshot userSnapshot = querySnapshot.Documents[0];
                DocumentReference userRef = userSnapshot.Reference;

                // Delete the user document
                await userRef.DeleteAsync();

                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                string uid = userRecord.Uid;

                // Now you have the UID, you can delete the user
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);

                return Ok("User deleted successfully.");
            }
            else
            {
                return NotFound("User not found.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

}

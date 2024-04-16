using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Stepmeet_BE.Models;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly FirestoreDb _firestoreDb;
    private readonly StorageClient _storageClient;

    public UsersController(FirestoreDb firestoreDb, StorageClient storageClient)
    {
        _firestoreDb = firestoreDb;
        _storageClient = storageClient;
    }

    [HttpPost("{email}/profile-picture")]
    public async Task<ActionResult> UploadProfilePicture(string email, IFormFile file)
    {
        try
        {
            // Check if a file was uploaded
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file uploaded.");
            }
        
            // Specify the bucket name and object name
            var bucketName = "stepmeet-8ed5d.appspot.com"; // Replace with your actual bucket name
            var objectName = $"{email}-profile-picture"; // Adjust the object name as needed

            // Upload the image to Firebase Storage
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Specify the content type
                var contentType = file.ContentType;

                // Upload the image with specified content type
                _storageClient.UploadObject(bucketName, objectName, contentType, memoryStream);

                // Get the image URL
                var imageUrl = $"https://storage.googleapis.com/{bucketName}/{objectName}";

                // Update the user document with the image URL
                CollectionReference usersCollection = _firestoreDb.Collection("users");
                QuerySnapshot querySnapshot = await usersCollection.WhereEqualTo("email", email).GetSnapshotAsync();

                if (querySnapshot.Count > 0)
                {
                    DocumentSnapshot userSnapshot = querySnapshot.Documents[0];
                    DocumentReference userRef = userSnapshot.Reference;

                    // Update the user document with the new image URL
                    Dictionary<string, object> updates = new Dictionary<string, object>
                    {
                        { "dpUrl", imageUrl }
                    };
                    await userRef.UpdateAsync(updates);

                    return Ok("Profile picture uploaded successfully.");
                }
                else
                {
                    return NotFound("User not found.");
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpGet("{email}/profile-picture")]
    public async Task<ActionResult> GetProfilePicture(string email)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            CollectionReference usersCollection = _firestoreDb.Collection("users");
            QuerySnapshot querySnapshot = await usersCollection.WhereEqualTo("email", email).GetSnapshotAsync();

            if (querySnapshot.Count > 0)
            {
                DocumentSnapshot userSnapshot = querySnapshot.Documents[0];

                // Retrieve the profile picture object name from the user document
                if (userSnapshot.TryGetValue("dpUrl", out object dpUrl))
                {
                    string profilePictureUrl = dpUrl.ToString();

                    // Extract the object name from the profile picture URL
                    string objectName = profilePictureUrl.Substring(profilePictureUrl.LastIndexOf('/') + 1);

                    // Specify the bucket name
                    var bucketName = "stepmeet-8ed5d.appspot.com"; // Replace with your actual bucket name

                    // Download the image from Firebase Storage
                    using (var memoryStream = new MemoryStream())
                    {
                        _storageClient.DownloadObject(bucketName, objectName, memoryStream);

                        // Return the image data
                        return File(memoryStream.ToArray(), "image/jpeg"); // Adjust the content type as needed
                    }
                }
                else
                {
                    return NotFound("Profile picture URL not found for the user.");
                }
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




    [HttpGet("{email}/favorites")]
    public async Task<ActionResult<List<int>>> GetFavoriteTrailIds(string email)
    {
        try
        {
            // Access Firestore and fetch favorite trail IDs for the user
            CollectionReference usersCollection = _firestoreDb.Collection("users");
            QuerySnapshot querySnapshot = await usersCollection.WhereEqualTo("email", email).GetSnapshotAsync();

            if (querySnapshot.Count > 0)
            {
                DocumentSnapshot userSnapshot = querySnapshot.Documents[0];
                Dictionary<string, object>? userData = userSnapshot.ToDictionary();

                if (userData != null && userData.ContainsKey("favourites"))
                {
                    var favorites = userData["favourites"];
                        if (favorites is List<int> favoriteTrailIds)
                        {
                            return Ok(favoriteTrailIds);
                        }
                        else if (favorites is List<object> favoriteTrailIdsObjects)
                        {
                            try
                            {
                                // Convert the list of objects to a list of integers
                                List<int> favourites = favoriteTrailIdsObjects.Select(o => Convert.ToInt32(o)).ToList();
                                return Ok(favourites);
                            }
                            catch (FormatException)
                            {
                                return BadRequest("Invalid format for favorite trail IDs.");
                            }
                        }
                        else
                        {
                            return BadRequest("Invalid format for favorite trail IDs.");
                        }
                    }
            }

            return NotFound("User not found or favorite trail IDs not found for the user.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpPost("{email}/favorites/{newFavoriteId}")]
    public async Task<ActionResult> AddFavoriteTrailId(string email, int newFavoriteId)
    {
        try
        {
            // Access Firestore and fetch the user document with the given email
            CollectionReference usersCollection = _firestoreDb.Collection("users");
            QuerySnapshot querySnapshot = await usersCollection.WhereEqualTo("email", email).GetSnapshotAsync();

            if (querySnapshot.Count > 0)
            {
                // Get the reference to the first user document with the matching email
                DocumentSnapshot userSnapshot = querySnapshot.Documents[0];
                DocumentReference userRef = userSnapshot.Reference;

                // Get the current list of favorite trail IDs
                List<int> existingFavorites = userSnapshot.GetValue<List<int>>("favourites") ?? new List<int>();

                // Add the new favorite trail ID to the list
                existingFavorites.Add(newFavoriteId);

                // Update the user document with the updated list of favorite trail IDs
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                {"favourites", existingFavorites}
            };
                await userRef.UpdateAsync(updates);

                return Ok("New favorite trail ID added successfully.");
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

    [HttpDelete("{email}/favorites/{favoriteId}")]
    public async Task<ActionResult> DeleteFavoriteTrailId(string email, int favoriteId)
    {
        try
        {
            // Access Firestore and fetch the user document with the given email
            CollectionReference usersCollection = _firestoreDb.Collection("users");
            QuerySnapshot querySnapshot = await usersCollection.WhereEqualTo("email", email).GetSnapshotAsync();

            if (querySnapshot.Count > 0)
            {
                // Get the reference to the first user document with the matching email
                DocumentSnapshot userSnapshot = querySnapshot.Documents[0];
                DocumentReference userRef = userSnapshot.Reference;

                // Get the current list of favorite trail IDs
                List<int> existingFavorites = userSnapshot.GetValue<List<int>>("favourites") ?? new List<int>();

                // Check if the favorite ID exists in the list
                if (existingFavorites.Contains(favoriteId))
                {
                    // Remove the favorite trail ID from the list
                    existingFavorites.Remove(favoriteId);

                    // Update the user document with the updated list of favorite trail IDs
                    Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    {"favourites", existingFavorites}
                };
                    await userRef.UpdateAsync(updates);

                    return Ok("Favorite trail ID deleted successfully.");
                }
                else
                {
                    return NotFound("Favorite trail ID not found in the user's favorites list.");
                }
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

    [HttpPost("{email}/completed/{trailID}")]
    public async Task<ActionResult> AddCompletedTrail(string email, int trailID)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .Limit(1)
                .GetSnapshotAsync();

            // Check if a document is found with the provided email
            if (querySnapshot.Count > 0)
            {
                // Get the document reference corresponding to the email
                DocumentSnapshot userSnapshot = querySnapshot.Documents.First();
                DocumentReference userRef = userSnapshot.Reference;

                // Get the current list of completed trail IDs
                List<int> completedTrails = userSnapshot.GetValue<List<int>>("completed") ?? new List<int>();

                // Add the new completed trail ID to the list
                completedTrails.Add(trailID);

                // Update the user document with the updated list of completed trail IDs
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    {"completed", completedTrails}
                };
                await userRef.UpdateAsync(updates);

                return Ok("Trail marked as completed successfully.");
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

    [HttpGet("{email}/completed")]
    public async Task<ActionResult<List<int>>> GetCompletedTrails(string email)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .Limit(1)
                .GetSnapshotAsync();

            // Check if a document is found with the provided email
            if (querySnapshot.Count > 0)
            {
                // Get the document reference corresponding to the email
                DocumentSnapshot userSnapshot = querySnapshot.Documents.First();

                // Get the list of completed trail IDs
                List<int> completedTrails = userSnapshot.GetValue<List<int>>("completed") ?? new List<int>();

                return Ok(completedTrails);
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

    [HttpPost("{email}/completed/{trailID}/comments")]
    public async Task<ActionResult> AddCommentToCompletedTrail(string email, long trailID, [FromBody] string comment)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .Limit(1)
                .GetSnapshotAsync();

            // Check if a document is found with the provided email
            if (querySnapshot.Count > 0)
            {
                // Get the document reference corresponding to the email
                DocumentSnapshot userSnapshot = querySnapshot.Documents.First();
                DocumentReference userRef = userSnapshot.Reference;

                // Get the current list of completed trail comments
                List<Dictionary<string, object>> completedTrailComments;

                // Check if the 'comments' field exists and retrieve its value if present
                if (userSnapshot.ContainsField("comments"))
                {
                    completedTrailComments = userSnapshot.GetValue<List<Dictionary<string, object>>>("comments");
                }
                else
                {
                    // If the 'comments' field doesn't exist, initialize it as an empty list
                    completedTrailComments = new List<Dictionary<string, object>>();
                }

                // Create a new comment object
                Dictionary<string, object> commentObject = new Dictionary<string, object>
            {
                {"trailID", trailID},
                {"email", email},
                {"comment", comment}
            };

                // Add the new comment object to the list of completed trail comments
                completedTrailComments.Add(commentObject);

                // Update the user document with the updated list of comments
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                {"comments", completedTrailComments}
            };
                await userRef.UpdateAsync(updates);

                return Ok("Comment added to completed trail successfully.");
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


    [HttpGet("{email}/comments")]
    public async Task<ActionResult<List<Dictionary<string, object>>>> GetCommentsByEmail(string email)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .Limit(1)
                .GetSnapshotAsync();

            // Check if a document is found with the provided email
            if (querySnapshot.Count > 0)
            {
                // Get the document reference corresponding to the email
                DocumentSnapshot userSnapshot = querySnapshot.Documents.First();

                // Check if the 'comments' field exists and retrieve its value if present
                if (userSnapshot.ContainsField("comments"))
                {
                    List<Dictionary<string, object>> comments = userSnapshot.GetValue<List<Dictionary<string, object>>>("comments");
                    return Ok(comments);
                }
                else
                {
                    return NotFound("Comments not found for this user.");
                }
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

    [HttpDelete("{email}/comments/{commentId}")]
    public async Task<ActionResult> DeleteComment(string email, int commentId)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .Limit(1)
                .GetSnapshotAsync();

            // Check if a document is found with the provided email
            if (querySnapshot.Count > 0)
            {
                // Get the document reference corresponding to the email
                DocumentSnapshot userSnapshot = querySnapshot.Documents.First();

                // Check if the 'comments' field exists and retrieve its value if present
                if (userSnapshot.ContainsField("comments"))
                {
                    // Get the comments array
                    List<Dictionary<string, object>> comments = userSnapshot.GetValue<List<Dictionary<string, object>>>("comments");

                    // Check if the commentId is within the bounds of the comments array
                    if (commentId >= 0 && commentId < comments.Count)
                    {
                        // Remove the comment from the comments array
                        comments.RemoveAt(commentId);

                        // Update the user document in Firestore with the modified comments array
                        await userSnapshot.Reference.UpdateAsync("comments", comments);

                        return Ok("Comment deleted successfully.");
                    }
                    else
                    {
                        return NotFound("Comment index out of range.");
                    }
                }
                else
                {
                    return NotFound("Comments not found for this user.");
                }
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



    [HttpGet("search/{name}")]
    public async Task<ActionResult<List<Dictionary<string, object>>>> SearchUsersByName(string name)
    {
        try
        {
            // Convert the substring to lowercase for case-insensitive search
            string searchQuery = name.ToLower();

            // Query Firestore to find all users
            QuerySnapshot usersQuerySnapshot = await _firestoreDb.Collection("users").GetSnapshotAsync();

            // Filter users whose firstName or lastName contains the searchQuery as a substring
            List<Dictionary<string, object>> usersData = usersQuerySnapshot
            .Documents
            .Select(doc => doc.ToDictionary())
            .Where(userData =>
                (((string)userData["firstName"]).ToLower().Contains(searchQuery) ||
                ((string)userData["lastName"]).ToLower().Contains(searchQuery)))
            .ToList();

            // Check if any users match the search query
            if (usersData.Count > 0)
            {
                return Ok(usersData);
            }
            else
            {
                return NotFound("No users found matching the search query.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpPatch("{email}/privacy/toggle")]
    public async Task<ActionResult> TogglePrivacy(string email)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .Limit(1)
                .GetSnapshotAsync();

            if (querySnapshot.Count > 0)
            {
                // Get the reference to the first user document with the matching email
                DocumentSnapshot userSnapshot = querySnapshot.Documents.First();
                DocumentReference userRef = userSnapshot.Reference;

                // Get the current value of isPrivate
                bool isPrivate = userSnapshot.GetValue<bool>("isPrivate");

                // Toggle the value of isPrivate
                bool newIsPrivate = !isPrivate;

                // Update the user document with the new value of isPrivate
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "isPrivate", newIsPrivate }
            };
                await userRef.UpdateAsync(updates);

                return Ok($"Privacy setting toggled successfully. New value: {newIsPrivate}");
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

    [HttpPost("feedback/{feedbackText}")]
    public async Task<IActionResult> PostFeedback(string feedbackText)
    {
        try
        {
            // Save the feedback text to the "feedback" collection in Firestore
            DocumentReference feedbackRef = _firestoreDb.Collection("feedback").Document();
            await feedbackRef.SetAsync(new { Text = feedbackText }); // Wrapping the text in an anonymous object

            return Ok("Feedback submitted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    [HttpPut("{email}/following")]
    public async Task<IActionResult> UpdateFollowing(string email, [FromBody] List<string> following)
    {
        try
        {
            // Query Firestore to find the user document with the provided email
            QuerySnapshot querySnapshot = await _firestoreDb.Collection("users")
                .WhereEqualTo("email", email)
                .GetSnapshotAsync();

            if (querySnapshot.Count > 0)
            {
                // Get the reference to the user document
                DocumentSnapshot userSnapshot = querySnapshot.Documents[0];
                DocumentReference userRef = userSnapshot.Reference;

                // Update the following array in the user document
                Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "following", following }
            };
                await userRef.UpdateAsync(updates);

                return Ok("Following updated successfully.");
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

    [HttpGet("{email}/following")]
    public async Task<IActionResult> GetFollowingUsers(string email)
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

                // Get the following array from the user document
                List<string> following = userSnapshot.GetValue<List<string>>("following");

                if (following != null && following.Count > 0)
                {
                    // Fetch the user documents for all users being followed
                    List<Dictionary<string, object>> followedUsers = new List<Dictionary<string, object>>();

                    foreach (string followingEmail in following)
                    {
                        QuerySnapshot followedQuerySnapshot = await _firestoreDb.Collection("users")
                            .WhereEqualTo("email", followingEmail)
                            .GetSnapshotAsync();

                        foreach (DocumentSnapshot followedUserSnapshot in followedQuerySnapshot)
                        {
                            Dictionary<string, object> followedUser = followedUserSnapshot.ToDictionary();
                            followedUsers.Add(followedUser);
                        }
                    }

                    return Ok(followedUsers);
                }
                else
                {
                    return Ok("No users being followed.");
                }
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

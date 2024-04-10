using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly FirestoreDb _firestoreDb;

    public UsersController(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
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



}

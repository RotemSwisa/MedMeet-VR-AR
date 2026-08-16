using UnityEngine;
using Normal.Realtime;

public class AvatarSelector : MonoBehaviour
{
    [Header("Avatar Prefabs")]
    public GameObject maleAvatarPrefab;    // DoctorPlayer
    public GameObject femaleAvatarPrefab;  // Femaledoctor

    private Realtime realtime;

    void Start()
    {
        realtime = GetComponent<Realtime>();

        if (realtime != null)
        {
            // קריאה לבחירת המגדר מה-PlayerPrefs
            string selectedGender = PlayerPrefs.GetString("SelectedGender", "Male");

            // בחירת ה-Prefab הנכון
            GameObject selectedAvatar = selectedGender == "Female" ? femaleAvatarPrefab : maleAvatarPrefab;

            // עדכון ה-Avatar Prefab של Realtime
            RealtimeAvatarManager avatarManager = realtime.GetComponent<RealtimeAvatarManager>();
            if (avatarManager != null)
            {
                avatarManager.localAvatarPrefab = selectedAvatar;
            }

            Debug.Log("Avatar selected: " + selectedGender);
        }
    }
}
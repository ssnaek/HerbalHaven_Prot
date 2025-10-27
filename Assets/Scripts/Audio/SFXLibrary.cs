using UnityEngine;

/// <summary>
/// ScriptableObject that stores all game sound effects.
/// Create via: Right-click > Create > Audio > SFX Library
/// </summary>
[CreateAssetMenu(fileName = "SFXLibrary", menuName = "Audio/SFX Library")]
public class SFXLibrary : ScriptableObject
{
    [Header("Player Sounds")]
    public AudioClip footstepWalk;
    public AudioClip footstepRun;
    public AudioClip jump;
    public AudioClip land;

    [Header("Interaction Sounds")]
    public AudioClip plantPickup;
    public AudioClip itemCollect;
    public AudioClip doorOpen;
    public AudioClip buttonClick;
    public AudioClip shopOpen;
    public AudioClip shopClose;

    [Header("UI Sounds")]
    public AudioClip uiOpen;
    public AudioClip uiClose;
    public AudioClip uiSelect;
    public AudioClip uiConfirm;
    public AudioClip uiCancel;

    [Header("Transition Sounds")]
    public AudioClip sceneTransitionStart;
    public AudioClip sceneTransitionEnd;
    public AudioClip teleport;

    [Header("Nature Sounds")]
    public AudioClip bushRustle;
    public AudioClip waterSplash;
    public AudioClip birdChirp;

    [Header("Misc")]
    public AudioClip errorSound;
    public AudioClip successSound;
}

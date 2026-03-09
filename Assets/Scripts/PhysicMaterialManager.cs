using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicMaterialManager : MonoBehaviour
{
    /*
    CHECK LATER HOW TO DISPLAY STRUCTS IN INSPECTOR

    public struct MaterialSounds
    {
        PhysicMaterial material;
        AudioClip footstep1Sound;
        AudioClip footstep2Sound;
        AudioClip footstep3Sound;
        AudioClip jumpSound;
        AudioClip landSound;
        AudioClip hitSound;
    }
    */

    [SerializeField] PhysicsMaterial[] materials;
    [SerializeField] AudioClip[] footstep1Sounds;
    [SerializeField] AudioClip[] footstep2Sounds;
    [SerializeField] AudioClip[] footstep3Sounds;
    [SerializeField] AudioClip[] jumpSounds;
    [SerializeField] AudioClip[] landSounds;
    [SerializeField] AudioClip[] hitSounds;

    AudioClip[] soundArray;

    string materialName;
    string audioClipName;

    public AudioClip[] GetSounds(PhysicsMaterial material)
    {
        materialName = material.name;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i].name == materialName
                || materials[i].name + " (Instance)" == materialName)
            {
                soundArray = new AudioClip[6];

                if (footstep1Sounds[i] != null)
                    soundArray[0] = footstep1Sounds[i];
                
                if (footstep2Sounds[i] != null)
                    soundArray[1] = footstep2Sounds[i];
                
                if (footstep3Sounds[i] != null)
                    soundArray[2] = footstep3Sounds[i];
                
                if (jumpSounds[i] != null)
                    soundArray[3] = jumpSounds[i];
                
                if (landSounds[i] != null)
                    soundArray[4] = landSounds[i];
                
                if (hitSounds[i] != null)
                    soundArray[5] = hitSounds[i];

                return soundArray;
            }
        }

        return new AudioClip[0];
    }

    public PhysicsMaterial GetMaterial(AudioClip audioClip)
    {
        audioClipName = audioClip.name;

        for (int i = 0; i < footstep1Sounds.Length; i++)
        {
            if (footstep1Sounds[i].name == audioClipName
                || footstep2Sounds[i].name == audioClipName
                || footstep3Sounds[i].name == audioClipName
                || jumpSounds[i].name == audioClipName
                || landSounds[i].name == audioClipName
                || hitSounds[i].name == audioClipName)
                return materials[i];
        }

        return null;
    }
}

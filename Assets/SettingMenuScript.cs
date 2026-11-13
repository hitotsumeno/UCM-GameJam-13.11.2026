using UnityEngine;

public class SettingMenuScript : MonoBehaviour
{
    public void setMasterVolume(float volume)
    {
        Debug.Log("Volume level:" + volume);
        // TODO: Add Volume mixer
    }
    public void setSFXVolume(float volume)
    {
        Debug.Log("Volume level:" + volume);
        // TODO: Add Volume mixer
    }
    public void setFullScreen(bool isFullscreen)
    {
        Debug.Log("Fullscreen:" + isFullscreen);
        Screen.fullScreen = isFullscreen;
    }
}

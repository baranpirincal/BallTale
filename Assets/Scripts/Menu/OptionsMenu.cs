﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : SimpleMenu<OptionsMenu>
{

	public TextMeshProUGUI soundSliderVal;
	public Slider soundSlider;
	
	public TextMeshProUGUI musicSliderVal;
	public Slider musicSlider;
	
	protected override void Awake()
	{
		base.Awake();

		
		UpdateSliderAndVal(soundSlider, soundSliderVal, PlayerPrefs.GetInt("SoundVolume"));
		UpdateSliderAndVal(musicSlider, musicSliderVal, PlayerPrefs.GetInt("MusicVolume"));
		
	}
	


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void UpdateSliderAndVal(Slider slider, TextMeshProUGUI val, int value)
	{
		slider.value = value;
		val.text = (value * 10).ToString();
	}
	
	public void OnSoundValueChanged()
	{
		
		PlayerPrefs.SetInt("SoundVolume", (int) soundSlider.value);
		
		var newVal = soundSlider.value * 10;
		soundSliderVal.text = newVal.ToString();
		
		AudioManager.Instance.SetSoundVolume(PlayerPrefs.GetInt("SoundVolume") / 10f);
		
	}
	public void OnMusicValueChanged()
	{
		
		PlayerPrefs.SetInt("MusicVolume", (int) musicSlider.value);
		var newVal = musicSlider.value * 10;
		musicSliderVal.text = newVal.ToString();
		
		AudioManager.Instance.SetMusicVolume(PlayerPrefs.GetInt("MusicVolume") / 10f);

	}

	public void DeleteSaveData()
	{
		DeleteConfirmationMenu.Show();
	}
	
	

}

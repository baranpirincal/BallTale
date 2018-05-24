﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Deals with all the bureaucracy behind bossfights : making camera static, blocking entrances and so on...
/// </summary>
public class Bossfight : MonoBehaviour
{

	[SerializeField] private GameObject _boss;
	[SerializeField] private List<GameObject> _walls;

	[SerializeField] private bool _takeCameraControl;
	[SerializeField] private Transform _cameraMiddle; // where to put the camera during the fight
	[SerializeField] private GameObject _booster;
	private bool _fightOn;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			StartFight();
		}
	}

	private void StartFight()
	{

		
		if (!_fightOn)
		{
			
			//AudioManager.Instance.StopAllMusic();

			StartCoroutine(AudioManager.Instance.FadeToNextMusic("02VortexBoss", 2.0f));
			
		//	AudioManager.Instance.Play("02VortexBoss", isLooping:true);
			_boss.SetActive(true);
		
			if (_takeCameraControl)
			{
				Camera.main.GetComponent<CameraFollow>().enabled = false;

				StartCoroutine(MoveCameraToPos(Camera.main.transform.position, _cameraMiddle.position));
			
			}

			StartCoroutine(ActivateWalls());
			_fightOn = true;
		}

	}

	private IEnumerator ActivateWalls()
	{
		yield return new WaitForSeconds(0.1f);
		
		_walls.ForEach(go => go.SetActive(true));
	}

	IEnumerator MoveCameraToPos(Vector3 startPos, Vector3 endPos, bool cameraFollowEnabledAfter = false)
	{
		
		float secondsMove = 1.5f;
		float timer = 0;

		while ((timer += Time.unscaledDeltaTime) < secondsMove)
		{
			Camera.main.transform.position = Vector3.Lerp(startPos, endPos, timer / secondsMove);

			yield return null;
		}

		if (cameraFollowEnabledAfter)
		{
			Camera.main.GetComponent<CameraFollow>().enabled =  true;
		}


		
	}

	public void Endfight()
	{
		if (_boss && _boss.GetComponent<Boss>().CurrentHealth < 1)
		{
			if (_takeCameraControl)
			{
				var booster = GameObject.FindGameObjectWithTag("boosttimer");
				
				StartCoroutine(MoveCameraToPos(Camera.main.transform.position, _booster.transform.position , true));
				
			}
			
			
			
			
			// FIGHTON FALSE TODO
		//	_walls.ForEach(go => go.SetActive(false));
		}

	}
}

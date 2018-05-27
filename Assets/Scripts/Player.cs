﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BayatGames.SaveGameFree;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CircleController2D))]
public class Player : MonoBehaviour
{

	[SerializeField] private int maxHealth = 100;
	[SerializeField] private float secondsInvincibility = 1.5f;

	[SerializeField] private int killBounceEnergy = 15;
	
	[SerializeField] private float shotSpeed;
	[SerializeField] private float shotCoolDown = 1.0f;
	[SerializeField] private float maxShotRange;
	
	[SerializeField] private float minJumpHeight = 1f;
	[SerializeField] private float maxJumpHeight = 4f;
	[SerializeField] private float timeToJumpApex = .4f;
	
	[SerializeField] private float moveSpeed = 10;
	
	[SerializeField] private float accelerationTimeAirborne = .2f;
	[SerializeField] private float accelerationTimeGrounded = .1f;
	
	[SerializeField] private float maxBoostTime = 2.0f;
	[SerializeField] private float boostForce = 20f;
	[SerializeField] private GameObject boostArrow;
	[SerializeField] private GameObject boostTimer;
	[SerializeField] private Image boostTimerFill;
	
	
	[SerializeField] private GameObject deathScreen;
	[SerializeField] private GameObject shootParticle;
	
	[SerializeField] private int currency = 100;


	public bool HasShotUpgrade = false;
	public bool IgnoreGround = false;
	
	private float _lastFacingDirection;
	
	private float _maxJumpVelocity;
	private float _minJumpVelocity;
	private float _velocityXSmoothing;

	private float _currentBoostTime = 0f;
	
	public Vector3 _velocity;
	
	private CircleController2D _controller;

	private Animator _animator;
	private Animator _arrowAnimator;
	
	private bool _canBoost = true;
	
	private int _currentHealth;
	private double _shotCoolDownTimer;
	private bool _isBoosting;

	private Vector3 _lastInput;

	public int Currency
	{
		get { return currency; }
		set { currency = value; }
	}
	
	public int CurrentHealth
	{
		get
		{
			return _currentHealth;
		}
		set
		{
			if (value < 1) // dead
			{
				StartCoroutine(Death());
			}
			else
			{
				_currentHealth = value;
			}
		}
	}

	public int MaxHealth
	{
		get { return maxHealth; }
		set { maxHealth = value; }
	}


	IEnumerator Death()
	{		
		deathScreen.SetActive(true);
		
		AudioManager.Instance.StopAllMusic();
		AudioManager.Instance.SetSoundVolume(0);
		
		yield return new WaitUntil((() => Input.GetKeyDown(KeyCode.Return)));


		if (SaveGame.Exists("player.txt"))
		{
			PlayerPrefs.SetInt("loadgame",1);
		}
		deathScreen.SetActive(false);
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
		
		
	}


	// Use this for initialization
	void Start ()
	{
		AudioManager.Instance.Play("01Peaceful", isLooping: true);
		
		deathScreen.SetActive(false);
		
		_currentHealth = maxHealth;

		_animator = GetComponent<Animator>();
		
		if (boostArrow)
		{
			boostArrow.SetActive(false);
		}

		if (boostTimer)
		{
			boostTimer.SetActive(false);	
		}

		_arrowAnimator = boostArrow.GetComponent<Animator>();
		_controller = GetComponent<CircleController2D>();

		float gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
	
		Physics2D.gravity = new Vector3(gravity, 0,0);
		
		_maxJumpVelocity = Mathf.Abs(gravity * timeToJumpApex);
		_minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
	}


	// Update is called once per frame
	void Update ()
	{

		if (Time.timeScale > 0.01f)
		{
			UpdateMovement();
			CheckShooting();
			
			if (Math.Abs(_velocity.x) > .01f)
			{
				_lastFacingDirection = Mathf.Sign(_velocity.x);
	
			}

		}

	}

	private void CheckShooting()
	{
		if (!HasShotUpgrade) return;

		if (Input.GetButtonDown("Fire3") && shotCoolDown < _shotCoolDownTimer)
		{
			
			AudioManager.Instance.Play("Shot",0.7f);
			
			var particle = Instantiate(shootParticle, transform.position, Quaternion.identity);
			
			var shot = particle.GetComponent<Shot>();

			shot.MoveSpeed = shotSpeed;
			shot.Direction = _lastFacingDirection;
			shot.MaxRange = maxShotRange;

			_shotCoolDownTimer = 0;
			
		}
		
		_shotCoolDownTimer += Time.deltaTime;



	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.CompareTag("Killcollider") && !_animator.GetBool("Damaged"))
		{
			AudioManager.Instance.Play("MonsterHit");
			
			var enemy = other.transform.parent.gameObject;

			Currency += enemy.GetComponent<BasicEnemy>().CurrencyOnKill;
			Destroy(enemy);

			_velocity.y = 0;
			_velocity.y += killBounceEnergy;
		}


		if (other.gameObject.CompareTag("HitCollider") && !_animator.GetBool("Damaged"))
		{
			var boss = other.gameObject.GetComponentInParent<Boss>();

			if (boss.CurrentState == BossState.VULNERABLE)
			{
				boss.GetDamaged();
				_velocity.y = 0;
				_velocity.y += killBounceEnergy;
			}		

		}
		
	}
	
	// TODO : Rewrite this logic into each enemy script, rather than checking them in here
	private void OnTriggerStay2D(Collider2D other)
	{	

		if (other.gameObject.CompareTag("Enemy"))
		{
			var enemy = other.gameObject.GetComponent<BasicEnemy>();
			
			DamagePlayer(enemy.Damage);
		}
		else if (other.gameObject.CompareTag("Trap"))
		{
			var trap = other.gameObject.GetComponent<Trap>();

			
			DamagePlayer(trap.Damage);
			
		}
				
	}

	public void DamagePlayer(int damageToTake)
	{
		
		if (!_animator.GetBool("Damaged"))
		{	
			
			StartCoroutine(PlayerDamaged());
			CurrentHealth -= damageToTake;
			Debug.Log($"Took a hit, {_currentHealth} health left. ");
		}
	}

	public IEnumerator PlayerDamaged()
	{
		AudioManager.Instance.Play("PlayerDamaged");
		
		_animator.SetBool("Damaged", true);
		
		var randomXJitter = Random.Range(-1.5f,1.5f);
		var randomYJitter = Random.Range(0.5f, 1f); // TODO : Fix y boost not working on ground on damaged
		
		_velocity += new Vector3(randomXJitter, randomYJitter,0);
		
		
		var timer = secondsInvincibility;
		while (timer > .0f)
		{
			timer -= Time.deltaTime;
			yield return null;
		}
		
		_animator.SetBool("Damaged", false);


	}	

	
	private void UpdateMovement()
	{
		
		if (_controller.collisions.above || _controller.collisions.below)
		{
			if (!IgnoreGround)
			{
				_velocity.y = 0;
			}
			
			
		}

		if (_controller.collisions.below)
		{
			_canBoost = true;
		}
		
		Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		//input = new Vector2(Mathf.Round(input.x), Mathf.Round(input.y));
		//print(input);

		input = ConvertToInteger(input);
		
		
		if (input != Vector2.zero)
		{
			_lastInput = input;
			
		}

		Debug.DrawRay(transform.position, input, Color.yellow);

		float boostInput = Input.GetAxisRaw("Fire1");

		if (Input.GetButtonDown("Fire1")  && !_controller.collisions.below && _canBoost)
		{
			AudioManager.Instance.Play("BoostCharge");
		}
		

		
		if (boostInput != 0  && !_controller.collisions.below && _canBoost)
		{
			
			boostArrow.SetActive(true);
			boostTimer.SetActive(true);


			
			if (_currentBoostTime > maxBoostTime)
			{
				
				boostArrow.SetActive(false);
				boostTimer.SetActive(false);
				_canBoost = false;
				_isBoosting = false;
				_currentBoostTime = 0.0f;
				boostTimerFill.fillAmount = 0;
				return;
			}
			
			float angle = Mathf.Atan2(_lastInput.y, _lastInput.x) * Mathf.Rad2Deg;
			
			Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);

			
			if (!_isBoosting)
			{
				boostArrow.transform.rotation = q;
				_isBoosting = true;
			}
			else
			{
				boostArrow.transform.rotation = Quaternion.Slerp(boostArrow.transform.rotation, q, 8 * Time.deltaTime);
			}

			boostTimerFill.fillAmount = (_currentBoostTime / maxBoostTime) + 0.07f;
			
			_currentBoostTime += Time.deltaTime;
			
			
			return;
		}


		if (boostInput == 0 && !_controller.collisions.below && _canBoost && _isBoosting)
		{
			AudioManager.Instance.Stop("BoostCharge");
			AudioManager.Instance.Play("BoostFinish");
			
			_velocity = _lastInput * boostForce ;
			
			
			_currentBoostTime = 0.0f;
			_canBoost = false;
			_isBoosting = false;
			boostTimerFill.fillAmount = 0;
			boostArrow.SetActive(false);
			boostTimer.SetActive(false);
		}
		
		if (Input.GetButtonDown("Jump") && _controller.collisions.below)
		{
			AudioManager.Instance.Play("Jump", 0.5f);
			_velocity.y = _maxJumpVelocity;
		}

		if (Input.GetButtonUp("Jump") && _canBoost)
		{
			if (_velocity.y > _minJumpVelocity)
			{
				_velocity.y = _minJumpVelocity;
			}
			
		}

		float targetVelocityX = Mathf.Round(input.x) * moveSpeed;

		_velocity.x = Mathf.SmoothDamp(_velocity.x, targetVelocityX, ref _velocityXSmoothing, (_controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
		
		_velocity.y += Physics2D.gravity.x * Time.deltaTime;
		_controller.Move(_velocity * Time.deltaTime);
	}

	private Vector2 ConvertToInteger(Vector2 input)
	{

		if (input.x < 0)
		{
			input.x = -1;
		}

		if (input.x > 0)
		{
			input.x = 1;
		}

		if (input.y < 0)
		{
			input.y = -1;
		}

		if (input.y > 0)
		{
			input.y = 1;
		}

		return input;
	}
}

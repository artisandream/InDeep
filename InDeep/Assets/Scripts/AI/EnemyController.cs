using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
	private float velocity;//the current velocity of the enemy in x
	private float activationTime = 0.0F;//test this against the current time to activate a respawn
	private float nextActivate = 1.0F;// the next time an enemy can respawn


	public int health = 3;//the enemy health
	public int healthReturn = 3;//the value that the enemy should return to if upon respawning
	public int randomSpawningTime = 10;//the highest posible amount of time when the enemy can respawn
	
	public GameObject myTarget; // the location of the player the enemies case
	public GameObject art;//game art that be deactivated when the it "explodes"
	public GameObject explosion;//the FX for enemyExploding
	
	public Animator EnemyAnims;//drop an Animator component here
	public NavMeshAgent navMeshAgent; //accesses the navmesh component of an enemy
	

	public delegate void RunStartOnce();//used to start a new weaponList instance 
	public event RunStartOnce RunOnceEvent;//used to subscribe to any weapons classes that might me instanced in a level

	//sends value of score on death
	public int enemyPointValue = 10;
	public delegate void SendScoreDelegate(int _enemyPointValue);
	public static event SendScoreDelegate SendScoreOnDeathEvent;

	
	EnemyController()//the class contructor
	{
		RunOnceEvent += RunOnce;//subcribes to the RunOnceEvent event
	}
	
	void RunOnce()
	{
		RunOnceEvent -= RunOnce;//unsubscribes to the RunOnce event if the enemy is respawned
	}
	
	void Start()
	{
		navMeshAgent = this.GetComponent<NavMeshAgent>();//finds the required NavMeshAgent
		if (RunOnceEvent != null) //checks for subscribers
			RunOnceEvent();//raises the RunOnceEvent
	}
	
	void OnEnable()
	{
		myTarget = navMeshAgent.gameObject;//Sets the target to itself
		healthReturn = health;//sets the return health to the users current health value
	}
	
	void OnDisable()
	{

	}
	
	public void StartEnemyMove()
	{
		StartCoroutine(MoveEnemyToTarget());//replaced the Update call and only runs when called
	}
	
	
	IEnumerator MoveEnemyToTarget()//sets the destination of the NavMeshAgent to the player
	{
		navMeshAgent.destination = myTarget.transform.position;
		// set the destination of the enemy to follow the player
		velocity = navMeshAgent.velocity.x;
		//gets the velocity of the current agent
		EnemyAnims.SetFloat("Walk", velocity);
		yield return null;
	}
	//sets animation to swim or idle depending on the speed
	
	public void EndSwim()
	{
		EnemyAnims.SetFloat("Walk", 0);
	}
	
	IEnumerator Deactivate()
	{
		yield return new WaitForSeconds(1.5f);

		if(SendScoreOnDeathEvent != null) {
			SendScoreOnDeathEvent(enemyPointValue);
		}

		EnemyAnims.SetBool("Explode", false);
		this.gameObject.SetActive(false);//turns off the gameObject
		EnemyAnims.SetLayerWeight(2, 0f);
	}
	
	void Awake()
	{
		this.gameObject.SetActive(true);//turns off the gameObject
		StartCoroutine(Deactivate());
	}

	
	IEnumerator EndDamage()
	{
		var i = Random.Range(0.5f, 1);
		EnemyAnims.SetLayerWeight(2, i);
		EnemyAnims.SetBool("Damage", true);
		yield return new WaitForSeconds(0.2f);
		EnemyAnims.SetLayerWeight(2, 0);
		EnemyAnims.SetBool("Damage", false);
	}


	public void LowerHealth(Collider _c)
	{
		if (health <= 0) {//tests for current health value
			art.SetActive(false);
			explosion.SetActive(true);
			StartCoroutine(Deactivate());
		}
	}
	
	void Reactivate(Vector3 _v)
	{//the delegate passes a value of the location to respawn
		EnemyAnims.SetLayerWeight(2, 0);
		health = healthReturn;//resets the health var
		this.transform.position = new Vector3(_v.x, _v.y, _v.z);// places the enemy in the position that the delegate passes
		if (Time.time > activationTime) {
			art.SetActive(true);
			explosion.SetActive(false);
			this.gameObject.SetActive(true);//actiovates the enemy
			activationTime = Time.time + nextActivate + Random.Range(0, randomSpawningTime);//sets the next time it can be activated
		}
	}
	
	void OnTriggerEnter(Collider _c)
	{
		if (_c.gameObject.layer == 14) {//this should be the Player ammo
			LowerHealth(_c);
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
// Used as the base script for all weapons.

public class MeleeBaseState : State
{
    // How long this state should be active for before moving on
    public float duration;
    // Cached animator component
    protected Animator animator;
    // bool to check whether or not the next attack in the sequence should be played or not
    protected bool shouldCombo;
    // The attack index in the sequence of attacks
    protected int attackIndex;

    // What the next attack should be
    protected string nextAttack;

    protected float movementModifier;

    // The cached hit collider component of this attack
    protected Collider2D hitCollider;
    // Cached already struck objects of said attack to avoid overlapping attacks on same target
    private List<Collider2D> collidersDamaged;
    // The Hit Effect to Spawn on the afflicted Enemy
    private GameObject HitEffectPrefab;
    //Current weapon
    protected Weapon curWeapon;
    protected Target aimScript;
    protected PlayerMovement playerMovement;
    protected GameObject player;
    protected PlayerAnimate playerAnimate;

    // closest enemy direction and distance
    protected Vector2 enemyDirection;
    protected float enemyDistance;
    protected bool directionPicked;

    protected float saveX, saveY;


    // have we picked a target (used for attacks that attack over time, prevents switchign)
    protected bool targetAcquired;

    // Input buffer Timer
    // private float AttackPressedTimer = 0;

    // Allow attack cancel if we hit enemy
    protected bool hasHit;

    public void Start()
    {
        Debug.Log("?");

    }


    public override void OnEnter(StateMachine _stateMachine)
    {
        base.OnEnter(_stateMachine);
        Initialise();
        if (curWeapon.effectSound != null)
        {
            AudioSource.PlayClipAtPoint(curWeapon.effectSound, player.transform.position, 1f);
        }

        targetAcquired = false;
        hasHit = false;
        playerMovement.SetSpeedModifier(curWeapon.speedMultiplier);

        int newState = playerAnimate.GetAnimationState(curWeapon.weaponState);
        playerAnimate.ChangeAnimationState(newState);
        playerAnimate.SetAttacking(true);

        duration = playerAnimate.GetAnimationLength();
        AimTarget();


    }

    protected void Initialise()
    {
        animator = GetComponent<Animator>();
        playerAnimate = GetComponent<PlayerAnimate>();
        collidersDamaged = new List<Collider2D>();
        hitCollider = GetComponent<WeaponManager>().hitbox;
        HitEffectPrefab = GetComponent<WeaponManager>().Hiteffect;
        curWeapon = GetComponent<WeaponManager>().GetAttackingWeapon();
        aimScript = GetComponent<Target>();
        playerMovement = GetComponent<PlayerMovement>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (fixedtime >= duration)
        {
            if (shouldCombo)
            {
                stateMachine.SetNextState(new MeleeEntryState());
            }
            else
            {
                stateMachine.SetNextStateToMain();
            }
        }


        if (animator.GetFloat("Weapon.Active") > 0f)
        {
            Attack();
        }

    }

    public override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        MoveToTarget();
    }

    public override void OnExit()
    {
        playerAnimate.SetAttacking(false);
        playerMovement.ResetSpeedMultiplier();
        base.OnExit();
    }

    protected void Attack()
    {
        Collider2D[] collidersToDamage = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        int colliderCount = Physics2D.OverlapCollider(hitCollider, filter, collidersToDamage);

        for (int i = 0; i < colliderCount; i++)
        {
            if (!collidersDamaged.Contains(collidersToDamage[i]))
            {
                DamageEnemy(collidersToDamage[i]);

            }
        }
    }

    private void DamageEnemy(Collider2D collider)
    {
        TeamComponent hitTeamComponent = collider.GetComponentInChildren<TeamComponent>();

        

        if (hitTeamComponent && hitTeamComponent.teamIndex == TeamIndex.Enemy)
        {


            GameObject.Instantiate(HitEffectPrefab, collider.transform);

            Health health = collider.gameObject.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(curWeapon.damage);

                if (curWeapon.hitSound != null)
                {
                    AudioSource.PlayClipAtPoint(curWeapon.hitSound, collider.transform.position, 1f);
                }
            }

            hasHit = true;

            collidersDamaged.Add(collider);
        }
    }

    protected void AimTarget()
    {
        saveX = animator.GetFloat("AimX");
        saveY = animator.GetFloat("AimY");

        animator.SetFloat("SaveX", saveX);
        animator.SetFloat("SaveY", saveY);
    }



    protected void MoveToTarget()
    {
        if (curWeapon.momentum == 0)
            return;
        
        if (fixedtime < curWeapon.momentumDelay)
            return;
        
        if (!targetAcquired)
        {
            FindEnemy();
        }

        WeaponMovePlayer();
    }

    protected void FindEnemy()
    {
        enemyDirection = aimScript.GetAimVector();
        targetAcquired = true;
    }


    protected void WeaponMovePlayer()
    {
        Vector2 moveDirection = enemyDirection.normalized;
        Vector2 movementStep = moveDirection * curWeapon.momentum * Time.deltaTime;
        playerMovement.transform.position += new Vector3(movementStep.x, movementStep.y, 0);
    }
}
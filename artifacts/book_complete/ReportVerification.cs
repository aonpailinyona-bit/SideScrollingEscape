using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using SideScrollingEscape.Player;
using SideScrollingEscape.Enemy;
using SideScrollingEscape.Items;
using SideScrollingEscape.Core;
using SideScrollingEscape.Environment;
using SideScrollingEscape.Audio;
using SideScrollingEscape.UI;

[InitializeOnLoad]
public static class ReportVerificationLaunch {
 static ReportVerificationLaunch() { EditorApplication.delayCall += Start; EditorApplication.playModeStateChanged += Changed; }
 static string Dir => Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/book_complete"));
 static void Start() {
  if(File.Exists(Path.Combine(Dir,"verification.done"))) return;
  if(EditorApplication.isPlaying) { if(SessionState.GetBool("ReportVerification.active",false)) Boot(); return; }

  SessionState.SetBool("ReportVerification.active",true);
  EditorApplication.isPlaying=true;
 }
 static void Changed(PlayModeStateChange s) { if(s==PlayModeStateChange.EnteredPlayMode) Boot(); }
 static void Boot() { if(UnityEngine.Object.FindAnyObjectByType<ReportVerification>()==null) new GameObject("ReportVerificationTemporary").AddComponent<ReportVerification>(); }
}

public class ReportVerification : MonoBehaviour {
 [Serializable] public class Row { public string id,status,result; }
 [Serializable] public class Results { public string method="Unity Play Mode controlled component tests; no scene or game script saved"; public List<Row> rows=new List<Row>(); }
 Results results=new Results();
 string Dir => Path.GetFullPath(Path.Combine(Application.dataPath,"../../artifacts/book_complete"));
 const BindingFlags BF=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 PlayerController p; KillerAI k; PlayerHealth health; Rigidbody2D rb; Animator pa,ka; Keyboard keyboard;
 static object Get(object o,string n) { return o.GetType().GetField(n,BF).GetValue(o); }
 static void Set(object o,string n,object v) { var f=o.GetType().GetField(n,BF);if(f==null) f=o.GetType().GetField("<"+n+">k__BackingField",BF);f.SetValue(o,v); }
 static object Call(object o,string n,params object[] v) {return o.GetType().GetMethod(n,BF).Invoke(o,v);}
 void Check(string id,bool ok,string detail) {results.rows.Add(new Row{id=id,status=ok?"PASS":"FAIL",result=detail});File.WriteAllText(Path.Combine(Dir,"verification.json"),JsonUtility.ToJson(results,true));}
 void Test(string id,Func<string> f) {try {Check(id,true,f());}catch(Exception e){Check(id,false,(e.InnerException??e).Message);}}
 void Assert(bool v,string message) {if(!v)throw new Exception(message);}
 void Keys(params Key[] keys) {InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();}
 void ResetPlayer() {p.Unfreeze();p.SetHiddenState(false);p.SetInventoryState(false);p.ExitHurtState();p.EndTransition();health.Heal(100);Set(health,"IsInvincible",false);Keys();}
 void State(KillerState s) {Call(k,"SetState",s);}
 void Advance(Animator a,float seconds) {for(int i=0;i<Mathf.CeilToInt(seconds/.02f);i++) a.Update(.02f);}
 bool Anim(Animator a,string n) {return a.GetCurrentAnimatorStateInfo(0).IsName(n)||a.GetNextAnimatorStateInfo(0).IsName(n);}
 void Init() { var tutorial=UnityEngine.Object.FindAnyObjectByType<TutorialPromptController>(); if(tutorial!=null) Call(tutorial,"FinishClose"); if(GameManager.Instance==null) Instantiate(Resources.Load<GameManager>("GameManager")); Set(GameManager.Instance,"CurrentLevelIndex",SceneManager.GetActiveScene().name=="Level3"?2:0); Call(GameManager.Instance,"SetState",GameState.Playing);Time.timeScale=1;
  p=UnityEngine.Object.FindAnyObjectByType<PlayerController>();k=UnityEngine.Object.FindAnyObjectByType<KillerAI>();Assert(p!=null&&k!=null,"Player or Killer missing");
  foreach(var ai in UnityEngine.Object.FindObjectsByType<KillerAI>(FindObjectsSortMode.None)) ai.enabled=false;
  p.enabled=false;((InputActionMap)Get(p,"playerMap")).Enable();
  health=p.GetComponent<PlayerHealth>();rb=p.GetComponent<Rigidbody2D>();pa=p.GetComponent<Animator>();ka=k.GetComponent<Animator>();
  rb.gravityScale=0; k.GetComponent<Rigidbody2D>().gravityScale=0;
  p.transform.position=new Vector3(1000,1000,0);k.transform.position=new Vector3(1002,1000,0);Physics2D.SyncTransforms();
  Set(k,"canScream",false);Set(k,"isBlind",false);Set(k,"obstacleMask",(LayerMask)0);Set(k,"facing",-1);
  Set(InventoryManager.Instance,"level1JumpscareChance",0f);
 }
 IEnumerator Start() {
  DontDestroyOnLoad(gameObject);keyboard=InputSystem.AddDevice<Keyboard>();
  if(GameManager.Instance==null) Instantiate(Resources.Load<GameManager>("GameManager")); Set(GameManager.Instance,"CurrentLevelIndex",0); SceneManager.LoadScene("Level1");yield return null;yield return null;yield return null;
  try {Init();}catch(Exception e){Check("SETUP",false,e.Message);Finish();yield break;}
  yield return null; ResetPlayer();
  if(File.Exists(Path.Combine(Dir,"verification_supplement.request"))) {
   results=JsonUtility.FromJson<Results>(File.ReadAllText(Path.Combine(Dir,"verification.json"))); results.rows.RemoveAll(x=>x.id=="TC-FSM-E04"||x.id=="TC-10");
   Test("TC-FSM-E04",()=>{ResetPlayer();Set(k,"facing",-1);p.transform.position=k.transform.position+Vector3.left*2;Physics2D.SyncTransforms();bool seen=(bool)Call(k,"DetectPlayer");Assert(seen,"Sight detection failed in facing direction");State(KillerState.Patrol);Set(k,"suspicion",1f);Call(k,"TickPatrol",seen);if(k.State!=KillerState.Chase)Call(k,"TickAlert",true);Assert(k.State==KillerState.Chase,"No Chase");return "Player placed 2 units within facing FOV; DetectPlayer true; Patrol/Alert => Chase";});
   Set(GameManager.Instance,"CurrentLevelIndex",2);SceneManager.LoadScene("Level3");yield return null;yield return null;yield return null;Init();yield return null;
   var trap=UnityEngine.Object.FindAnyObjectByType<Level3NoiseTrap>();var clip=(AudioClip)Get(trap,"glassBreakClip");clip.LoadAudioData();yield return new WaitForSecondsRealtime(.5f);
   Test("TC-10",()=>{k.transform.position=trap.transform.position+Vector3.right;State(KillerState.Patrol);Set(k,"hasActiveSound",false);int voice=AudioManager.Instance!=null?(int)Get(AudioManager.Instance,"nextVoice"):-1;Call(trap,"OnTriggerEnter2D",p.GetComponent<Collider2D>());Assert((bool)Get(trap,"triggered")&&!trap.GetComponent<Collider2D>().enabled&&k.State==KillerState.Listening,"Trap/noise failed");object sound=Get(k,"activeSound");Assert(sound.GetType().GetField("Type").GetValue(sound).ToString()=="Glass","Wrong sound type");float[] audioData=new float[clip.samples*clip.channels];bool dataRead=clip.GetData(audioData,0);Assert(dataRead&&Array.Exists(audioData,x=>Mathf.Abs(x)>.0001f),"No loaded audio samples");if(AudioManager.Instance!=null){var voices=(List<AudioSource>)Get(AudioManager.Instance,"sfxVoices");Assert((int)Get(AudioManager.Instance,"nextVoice")!=voice&&voices[voice].isPlaying,"Pooled source did not start one-shot");}return "Trap triggers; collider disabled; Glass => Listening; full loaded clip has sound samples; pooled one-shot playing";});
   Finish();yield break;
  }
  Test("TC-01",()=>{Keys(Key.D);Call(p,"Update");for(int i=0;i<12;i++)Call(p,"FixedUpdate");float right=rb.linearVelocity.x;Call(p,"Update");int facingRight=p.FacingDirection;Keys(Key.A);Call(p,"Update");for(int i=0;i<24;i++)Call(p,"FixedUpdate");Call(p,"Update");Assert(right>0&&rb.linearVelocity.x<0&&facingRight==1&&p.FacingDirection==-1,"Direction or facing incorrect");return "D velocity="+right+"; A velocity="+rb.linearVelocity.x+"; facing +1/-1";});
  Test("TC-02",()=>{ResetPlayer();Keys(Key.D);Call(p,"Update");for(int i=0;i<12;i++)Call(p,"FixedUpdate");float walk=rb.linearVelocity.x;float stamina=p.CurrentStamina;Keys(Key.D,Key.LeftShift);Call(p,"Update");for(int i=0;i<12;i++)Call(p,"FixedUpdate");float sprint=rb.linearVelocity.x;State(KillerState.Patrol);Set(k,"hasActiveSound",false);k.transform.position=p.transform.position+Vector3.right;var step=p.GetComponent<FootstepSystem>();Call(step,"PlayStep");object sound=Get(k,"activeSound");var type=sound.GetType().GetField("Type").GetValue(sound);Assert(sprint>walk&&p.CurrentStamina<stamina&&type.ToString()=="Sprint","Sprint speed, stamina or sound incorrect");return "walk="+walk+" sprint="+sprint+" stamina "+stamina+" -> "+p.CurrentStamina+" sound=Sprint";});
  Test("TC-03",()=>{ResetPlayer();rb.linearVelocity=Vector2.zero;Set(p,"IsGrounded",true);Set(p,"lastJumpTime",-999f);Call(p,"OnJump",default(InputAction.CallbackContext));float first=rb.linearVelocity.y;Call(p,"OnJump",default(InputAction.CallbackContext));Assert(first>0&&Mathf.Approximately(first,rb.linearVelocity.y),"Jump repeated or no impulse");return "first jump velocity="+first+"; immediate airborne jump did not add impulse";});
  Test("TC-04",()=>{var go=new GameObject("TemporaryLadder",typeof(BoxCollider2D));var ladder=go.AddComponent<Ladder>();go.transform.position=p.transform.position;Set(p,"currentLadder",ladder);Set(p,"ladderOverlapCount",1);Call(p,"EnterClimb");Assert((bool)Get(p,"isClimbing")&&rb.gravityScale==0,"Did not enter ladder");Set(p,"MoveInput",Vector2.up);Call(p,"ClimbFixedUpdate",true);float up=rb.linearVelocity.y;Set(p,"MoveInput",Vector2.down);rb.position=new Vector2(rb.position.x,ladder.BottomY+2);Call(p,"ClimbFixedUpdate",true);float down=rb.linearVelocity.y;Call(p,"OnJump",default(InputAction.CallbackContext));Assert(up>0&&down<0&&!(bool)Get(p,"isClimbing")&&rb.gravityScale>0,"Ladder up/down/exit incorrect");Destroy(go);return "climb up="+up+" down="+down+"; jump detached and restored gravity";});
  Test("TC-05",()=>{ResetPlayer();var light=p.GetComponent<FlashlightController>();Set(light,"Battery",20f);light.SetOn(false,false);light.Toggle();float initial=light.Battery;Call(light,"Update");Assert(light.IsOn&&light.Battery<initial,"Battery did not drain");Set(light,"Battery",0f);Call(light,"Update");light.Toggle();Assert(!light.IsOn,"Empty battery still on");return "toggle on; battery drained; at zero turned off and cannot toggle on";});
  Test("TC-FSM-P01",()=>{ResetPlayer();var h=p.GetComponent<HidingSystem>();Set(h,"spotsInRange",1);h.ToggleHide();Assert(h.IsHidden&&p.State==PlayerState.Hidden&&p.MovementLocked,"Hide failed");return "ToggleHide => Hidden, movement locked";});
  Test("TC-FSM-P02",()=>{var h=p.GetComponent<HidingSystem>();h.ToggleHide();Assert(!h.IsHidden&&p.State==PlayerState.Normal&&!p.MovementLocked,"Reveal failed");return "ToggleHide => Normal, controls available";});
  Test("TC-FSM-P03",()=>{ResetPlayer();float before=health.CurrentHealth;health.TakeDamage(10);Assert(health.CurrentHealth==before-10&&p.State==PlayerState.Hurt,"Damage/Hurt failed");return "HP "+before+" -> "+health.CurrentHealth+"; entered Hurt; recovery checked after coroutine";});
  yield return new WaitForSecondsRealtime(2.2f);
  Check("TC-FSM-P03-RECOVERY",p.State==PlayerState.Normal,"state after 2.2 sec="+p.State);
  Test("TC-FSM-P04",()=>{ResetPlayer();p.Freeze(2);Assert(p.State==PlayerState.Frozen&&p.MovementLocked,"Freeze failed");Keys(Key.X);Call(p,"Update");Keys();Call(p,"Update");Keys(Key.X);Call(p,"Update");Assert(p.State==PlayerState.Normal&&!p.IsFrozen,"Mash recovery failed");return "Frozen; two X presses => Normal";});
  Test("TC-FSM-P05",()=>{ResetPlayer();p.SetInventoryState(true);Assert(p.State==PlayerState.UsingItem&&p.MovementLocked,"Inventory lock failed");p.SetInventoryState(false);Assert(p.State==PlayerState.Normal&&!p.MovementLocked,"Inventory unlock failed");return "UsingItem locks movement; closing restores Normal";});
  Test("TC-FSM-P06",()=>{Assert(p.BeginTransition()&&p.State==PlayerState.Transitioning&&!p.CanUseGameplayInput,"Transition lock failed");p.EndTransition();return "Transitioning blocks gameplay input";});
  Test("TC-FSM-E01",()=>{State(KillerState.Patrol);Set(k,"hasActiveSound",false);k.HearNoise(k.transform.position+Vector3.right,1f,NoiseType.Rock);Assert(k.State==KillerState.Listening,"Noise not selected");return "HearNoise Rock => Listening";});
  Test("TC-FSM-E02",()=>{Set(k,"soundStateTimer",0f);Call(k,"TickListening",false);Assert(k.State==KillerState.InvestigateSound,"Did not investigate sound");Call(k,"TickInvestigateSound",false);Assert(Mathf.Abs(k.GetComponent<Rigidbody2D>().linearVelocity.x)>0,"No movement toward sound");return "Listening timer ended => InvestigateSound; horizontal movement nonzero";});
  Test("TC-FSM-E03",()=>{Set(k,"lastKnownPosition",(Vector2)k.transform.position);Call(k,"TickInvestigateSound",false);Assert(k.State==KillerState.ConfirmSound,"No confirmation");return "Reached noise location => ConfirmSound";});
  Test("TC-FSM-E04",()=>{ResetPlayer();p.transform.position=k.transform.position+Vector3.left*2;Physics2D.SyncTransforms();bool seen=(bool)Call(k,"DetectPlayer");Assert(seen,"Sight detection failed");State(KillerState.Patrol);Call(k,"TickPatrol",seen);Set(k,"suspicion",1f);Call(k,"TickPatrol",seen);if(k.State!=KillerState.Chase)Call(k,"TickAlert",true);Assert(k.State==KillerState.Chase,"Did not enter Chase");return "Unobstructed player detected; Patrol/Alert => Chase";});
  Test("TC-FSM-E05",()=>{ResetPlayer();p.transform.position=k.transform.position+Vector3.right*.3f;Set(k,"screamCooldownTimer",100f);Set(k,"wallRepathTimer",0f);State(KillerState.Chase);Call(k,"TickChase",true);Assert(k.State==KillerState.Attack,"No attack state");Set(k,"attackTimer",0f);float before=health.CurrentHealth;Call(k,"TickAttack",true);Assert(health.CurrentHealth<before&&k.State==KillerState.AttackRecover,"No damage/recovery");return "Chase => Attack; HP "+before+" -> "+health.CurrentHealth+"; AttackRecover";});
  Test("TC-FSM-E06",()=>{float duration=(float)Get(k,"attackRecoverTimer");Assert(duration>0,"No cooldown");Call(k,"TickAttackRecover",true);Assert(k.State==KillerState.AttackRecover,"Recovery skipped");Set(k,"attackRecoverTimer",0f);Call(k,"TickAttackRecover",true);Assert(k.State==KillerState.Chase,"Recovery did not end");return "AttackRecover held before timer; timer ended => Chase";});
  Test("TC-FSM-E07",()=>{ResetPlayer();p.transform.position=k.transform.position+Vector3.right*20;State(KillerState.Chase);Set(k,"chaseCommitTimer",0f);Set(k,"chaseMemoryTimer",0f);Set(k,"lostDetectionTimer",2f);Set(k,"wallRepathTimer",0f);Call(k,"TickChase",false);Assert(k.State==KillerState.Investigate,"No investigation");Set(k,"searchTimer",0f);Call(k,"TickInvestigate",false);Assert(k.State==KillerState.Search,"No search");return "Lost target => Investigate; investigation timer ended => Search";});
  Test("TC-FSM-E08",()=>{Set(k,"searchTimer",0f);Call(k,"TickSearch",false);Assert(k.State==KillerState.ReturnToPatrol,"No return state");return "Search timeout => ReturnToPatrol";});
  Test("TC-FSM-E09",()=>{for(int i=0;i<3;i++){State(KillerState.Patrol);Set(k,"hasActiveSound",false);k.HearNoise(k.transform.position,1f,NoiseType.Rock);}State(KillerState.ConfirmSound);Set(k,"soundStateTimer",0f);Call(k,"TickConfirmSound",false);Assert(k.State==KillerState.Confused,"Repeated rock did not confuse");return "Repeated Rock three times; ConfirmSound => Confused";});
  Test("TC-FSM-E10",()=>{ResetPlayer();Set(k,"freezeInsteadOfStun",false);Call(k,"TriggerScream");Assert(k.State==KillerState.Scream&&p.MovementLocked,"Scream did not stun");return "TriggerScream => Scream; player control locked by stun";});
  yield return new WaitForSecondsRealtime(1.2f);ResetPlayer();
  // Evaluate original Animator controllers at controlled parameters, without modifying assets.
  Test("AN-P01",()=>{pa.Rebind();pa.SetBool("isWalking",false);Advance(pa,.3f);Assert(Anim(pa,"Idle"),"Idle not active");pa.SetBool("isWalking",true);Advance(pa,.4f);Assert(Anim(pa,"Walk"),"Walk not active");pa.SetBool("isWalking",false);Advance(pa,.4f);Assert(Anim(pa,"Idle"),"Did not return Idle");return "Idle -> Walk -> Idle with isWalking false/true/false";});
  Test("AN-K01",()=>{ka.Rebind();ka.SetBool("isWalking",true);ka.SetBool("isRunning",false);Advance(ka,.5f);Assert(Anim(ka,"Walk"),"Walk not active");ka.SetBool("isRunning",true);Advance(ka,.5f);Assert(Anim(ka,"Run"),"Run not active");return "isWalking true => Walk; isRunning true => Run";});
  Test("AN-K02",()=>{ka.Rebind();ka.SetBool("isWalking",false);ka.SetBool("isRunning",false);Advance(ka,.2f);ka.SetTrigger("attack1");Advance(ka,.12f);Assert(Anim(ka,"Attack1"),"Attack1 not entered");Advance(ka,3f);Assert(Anim(ka,"Idle"),"Attack1 did not return Idle");return "attack1 => Attack1; exited to Idle after clip";});
  Test("AN-K03",()=>{foreach(string state in new[]{"Idle","Walk","Run","Attack1"}){ka.Rebind();ka.SetBool("isWalking",false);ka.SetBool("isRunning",false);ka.Play(state,0,0);ka.Update(.02f);ka.SetTrigger("scream");Advance(ka,.05f);Assert(Anim(ka,"Scream"),"Scream not entered from "+state);Advance(ka,3f);Assert(Anim(ka,"Idle"),"Scream did not exit");}return "scream from Idle/Walk/Run/Attack1 => Scream => Idle";});
  Test("AN-K04",()=>{ka.Rebind();ka.SetBool("isWalking",true);ka.SetBool("isRunning",true);Advance(ka,.6f);Assert(Anim(ka,"Run"),"Run not active");ka.SetBool("isRunning",false);Advance(ka,.6f);Assert(Anim(ka,"Walk"),"Run did not settle on Walk");return "Run with both bools true; isRunning false => Walk";});
  Test("TC-06",()=>{var inventory=InventoryManager.Instance;int before=inventory.KeyCount;var item=UnityEngine.Object.FindAnyObjectByType<ItemPickup>();Assert(item!=null,"No pickup");Call(item,"Collect");Assert(inventory.KeyCount==before+1&&inventory.CollectedItems.Count>0,"Key count/list did not update");return "Collect => key count "+before+" -> "+inventory.KeyCount+"; inventory updated";});
  var door=UnityEngine.Object.FindAnyObjectByType<ExitDoor>();
  Test("TC-07",()=>{Assert(door.GetPrompt().Contains("Locked")&&door.State==ExitDoorState.Locked,"Locked prompt missing");return door.GetPrompt();});
  Test("TC-09",()=>{var inventory=InventoryManager.Instance;Set(inventory,"KeyCount",inventory.RequiredKeyCount);Call(door,"HandleKeyCountChanged",inventory.KeyCount,inventory.RequiredKeyCount);ResetPlayer();p.transform.position=door.transform.position;foreach(var ai in UnityEngine.Object.FindObjectsByType<KillerAI>(FindObjectsSortMode.None))ai.transform.position=p.transform.position+Vector3.right*100;
   Keys(Key.E);door.Interact(p.gameObject);Assert(door.State==ExitDoorState.Unlocking,"Unlock did not start");Set(door,"unlockProgress",.5f);Keys();Call(door,"Update");Assert(door.State==ExitDoorState.Ready&&door.UnlockProgress01==0,"Release cancel failed");
   Keys(Key.E);door.Interact(p.gameObject);Set(door,"unlockProgress",.5f);p.transform.position=door.transform.position+Vector3.right*5;Call(door,"Update");Assert(door.State==ExitDoorState.Ready,"Distance cancel failed");
   p.transform.position=door.transform.position;door.Interact(p.gameObject);Set(door,"unlockProgress",.5f);Call(door,"HandlePlayerDamaged",5f);Call(door,"Update");Assert(door.State==ExitDoorState.Ready,"Damage cancel failed");
   door.Interact(p.gameObject);k.transform.position=p.transform.position+Vector3.right;Call(door,"Update");Assert(door.State==ExitDoorState.Ready,"Danger cancel failed");return "Unlock cancelled and progress reset on release, distance, damage and nearby Killer";});
  Keys(Key.E);ResetPlayer();p.transform.position=door.transform.position;foreach(var ai in UnityEngine.Object.FindObjectsByType<KillerAI>(FindObjectsSortMode.None))ai.transform.position=p.transform.position+Vector3.right*100;Keys(Key.E);door.Interact(p.gameObject);
  float deadline=Time.realtimeSinceStartup+4;
  while(door.State==ExitDoorState.Unlocking&&Time.realtimeSinceStartup<deadline){Call(door,"Update");yield return null;}
  Check("TC-08",door.State==ExitDoorState.Completed&&GameManager.Instance.CurrentState==GameState.LevelComplete,"E held; door="+door.State+" game="+GameManager.Instance.CurrentState);
  Check("TC-FSM-P06-DOOR",p.State==PlayerState.Transitioning,"After door completed player="+p.State);
  // Fresh runtime scene for lethal damage, Level3 trap/random placement/audio tests.
  Set(GameManager.Instance,"CurrentLevelIndex",2);SceneManager.LoadScene("Level3");yield return null;yield return null;yield return null;Init();
  Test("TC-10",()=>{var trap=UnityEngine.Object.FindAnyObjectByType<Level3NoiseTrap>();Assert(trap!=null,"No trap");k.transform.position=trap.transform.position+Vector3.right;State(KillerState.Patrol);Set(k,"hasActiveSound",false);Call(trap,"OnTriggerEnter2D",p.GetComponent<Collider2D>());Assert((bool)Get(trap,"triggered")&&!trap.GetComponent<Collider2D>().enabled&&k.State==KillerState.Listening,"Trap did not trigger/noise");object sound=Get(k,"activeSound");Assert(sound.GetType().GetField("Type").GetValue(sound).ToString()=="Glass","Wrong noise type");var clip=(AudioClip)Get(trap,"glassBreakClip");bool playing=false;foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))if(source.clip==clip&&source.isPlaying)playing=true;Assert(clip!=null&&clip.length>0&&playing,"Audio playback did not start");return "Trap triggered once; collider disabled; Glass selected; assigned glass audio clip playback started";});
  Test("TC-11",()=>{var randomizer=UnityEngine.Object.FindAnyObjectByType<Level3RandomEvents>();Assert(randomizer!=null,"No randomizer");string prev="";bool changed=false;for(int round=0;round<5;round++){Call(randomizer,"BuildGroundPools");Call(randomizer,"RandomizeKeysAndTraps");Physics2D.SyncTransforms();string positions="";foreach(var key in UnityEngine.Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None)){positions+=key.transform.position.ToString();var hit=Physics2D.Raycast(key.transform.position,Vector2.down,3f,LayerMask.GetMask("Ground"));Assert(hit.collider!=null,"No floor below randomized key");}if(round>0&&positions!=prev)changed=true;prev=positions;}Assert(changed,"Positions did not vary across five runs");return "Five randomized placements; positions changed; raycasts found Ground below keys";});
  Test("TC-12",()=>{var warning=UnityEngine.Object.FindAnyObjectByType<Level3HeartbeatWarning>();Assert(warning!=null,"No heartbeat warning");foreach(var ai in UnityEngine.Object.FindObjectsByType<KillerAI>(FindObjectsSortMode.None))ai.transform.position=p.transform.position+Vector3.right*100;Set(warning,"intensity",0f);Call(warning,"RefreshIntensity");float far=(float)Get(warning,"intensity");k.transform.position=p.transform.position+Vector3.right;for(int i=0;i<10;i++)Call(warning,"RefreshIntensity");float near=(float)Get(warning,"intensity");Call(warning,"UpdateVignette");var image=(UnityEngine.UI.Image)Get(warning,"vignette");float[] data=new float[2048];((AudioClip)Get(warning,"heartbeatClip")).GetData(data,0);Assert(near>far&&image.color.a>0&&Array.Exists(data,x=>Mathf.Abs(x)>.0001f),"Intensity/vignette/audio failed");return "far intensity="+far+" near="+near+"; vignette alpha="+image.color.a+"; heartbeat samples nonzero; interval formula 1.35 -> 0.46 sec";});
  Test("TC-FSM-P07",()=>{ResetPlayer();health.TakeDamage(10);Set(health,"IsInvincible",false);health.TakeDamage(health.CurrentHealth+1);Assert(health.IsDead&&p.State==PlayerState.Dead&&GameManager.Instance.CurrentState==GameState.Dead,"Death chain failed");return "Damage depleted HP => Dead player and Dead game state";});
  Test("TC-13",()=>{Set(GameManager.Instance,"CurrentLevelIndex",GameManager.Instance.LevelCount-1);GameManager.Instance.NotifyLevelCompleted();GameManager.Instance.ContinueToNextLevel();Assert(GameManager.Instance.CurrentState==GameState.GameWon,"No GameWon");return "Final level NotifyLevelCompleted + ContinueToNextLevel => GameWon";});
  Finish();
 }
 void Finish() {Keys();if(keyboard!=null)InputSystem.RemoveDevice(keyboard);File.WriteAllText(Path.Combine(Dir,"verification.done"),DateTime.Now.ToString("O"));SessionState.SetBool("ReportVerification.active",false);EditorApplication.isPlaying=false;}
}




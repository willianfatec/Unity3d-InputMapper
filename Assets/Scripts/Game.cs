﻿using UnityEngine;
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using ws.winx.input;
using System.Runtime.Serialization;
using System.Linq;
using ws.winx.platform;
using ws.winx.devices;
using ws.winx.input.states;
using System.Collections;
using ws.winx.gui;
using ws.winx.unity;
using System.Timers;
using ws.winx.drivers;
using ws.winx.utils;


namespace ws.winx
{
    public class Game : MonoBehaviour
    {

        // What if you have 50 states. Would you need to define them manually. => Forget it about this
        //		static int idleState = Animator.StringToHash("Base Layer.Idle");	
        //		static int locoState = Animator.StringToHash("Base Layer.Locomotion");			// these integers are references to our animator's states
        //		static int jumpState = Animator.StringToHash("Base Layer.Jump");				// and are used to check state for various actions to occur
        //		static int jumpDownState = Animator.StringToHash("Base Layer.JumpDown");		// within our FixedUpdate() function below
        //		static int fallState = Animator.StringToHash("Base Layer.Fall");
        //		static int rollState = Animator.StringToHash("Base Layer.Roll");
        //		static int waveState = Animator.StringToHash("Layer2.Wave");





        //bool isConnected = usbI.Connect();
        //Debug.Log(isConnected);
        //USBHIDDRIVER.USB
        //bool isConnected = usbI.Connect();



        Animator animator = null;
        bool _settingsLoaded = false;
        private float vSliderValue;
        private ThrustmasterRGTFFDDevice device;
        private byte forceX;
        private Timer timer;
        private float vSliderValuePrev;
        IEnumerator runEffectEnumerator;
        ComplementaryFilter complementaryFuzer;
        float lastTime = -1f;
        public static double RAD_TO_DEG = 180 / Math.PI;
        public static double DEG_TO_RAD = Math.PI / 180;
        GameObject wiimote;
        Vector3 Angles;


        // Use this for initialization
        void Start()
        {
            complementaryFuzer = new ComplementaryFilter();
            wiimote = GameObject.Find("wiimote");
            Angles = new Vector3();

            animator = GameObject.FindObjectOfType<Animator>();

            vSliderValuePrev = vSliderValue = 128f;

            timer = new Timer(500.0);
            timer.Elapsed += new ElapsedEventHandler(onTimerElapsed);

            UserInterfaceWindow ui = this.GetComponent<UserInterfaceWindow>();


            //supporting devices with custom drivers
            //When you add them add specialized first then XInputDriver  then wide range supporting drivers UnityDriver
#if (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID)
            InputManager.AddDriver(new ThrustMasterDriver());
          //  InputManager.AddDriver(new WiiDriver());
           // InputManager.AddDriver(new XInputDriver());


            // InputManager.AddDriver(new ws.winx.platform.windows.XInputDriver());
#endif

            // !!!Postive аxes mapping only currently(need to find way to distinct postive from negative axis)

            // if(Application.isPlaying)
            //     InputManager.AddDriver(new UnityDriver());


            //if you want to load some states from .xml and add custom manually first load settings xml
            //!!!Application.streamingAssetPath gives "Raw" folder in web player

#if (UNITY_STANDALONE || UNITY_EDITOR ) && !UNITY_WEBPLAYER && !UNITY_ANDROID
            //UnityEngine.Debug.Log("Standalone");


            if (ui != null && ui.settingsXML == null)
            {//settingsXML would trigger internal loading mechanism (only for testing)

                InputManager.loadSettings(Path.Combine(Application.streamingAssetsPath, "InputSettings.xml"));



                ui.StateInputCombinations = InputManager.Settings.stateInputs;
            }


            manuallyAddStateAndHandlers();

#endif

			#region Load InputSettings.xml Android
#if UNITY_ANDROID


            Loader request = new Loader();


            if (Application.platform == RuntimePlatform.Android)
            {
                if (File.Exists(Application.persistentDataPath + "/" + "InputSettings.xml"))
                {

                    if (ui != null)
                    {
                        Debug.Log("Game>> Try to load from " + Application.persistentDataPath);
                        InputManager.loadSettings(Application.persistentDataPath + "/" + "InputSettings.xml");
                        ui.StateInputCombinations = InputManager.Settings.stateInputs;
                        manuallyAddStateAndHandlers();
                        return;

                    }
                }
                else
                {// content of StreamingAssets get packed inside .APK and need to be load with WWW
                    request.Add(Path.Combine(Application.streamingAssetsPath, "InputSettings.xml"));
                }


                request.LoadComplete += new EventHandler<LoaderEvtArgs>(onLoadComplete);
                request.Error += new EventHandler<LoaderEvtArgs>(onLoadItemComplete);
                request.LoadItemComplete += new EventHandler<LoaderEvtArgs>(onLoadItemComplete);
                request.load();
            }
            else //TARGET=ANDROID but playing in EDITOR => use Standalone setup
            {
                if (ui != null && ui.settingsXML == null)
                {//settingsXML would trigger internal loading mechanism (only for testing)

                    InputManager.loadSettings(Path.Combine(Application.streamingAssetsPath, "InputSettings.xml"));



                    ui.StateInputCombinations = InputManager.Settings.stateInputs;
                }


                manuallyAddStateAndHandlers();

            }
           

            
#endif
			#endregion

#if(UNITY_WEBPLAYER || UNITY_EDITOR) && !UNITY_STANDALONE && !UNITY_ANDROID
            Loader request = new Loader();

                //UNITY_WEBPLAYER: application path gives "http://localhost/appfolder/"
                request.Add(Application.dataPath+"/StreamingAssets/InputSettings.xml");
            

            request.LoadComplete += new EventHandler<LoaderEvtArgs>(onLoadComplete);
            request.Error += new EventHandler<LoaderEvtArgs>(onLoadItemComplete);
            request.LoadItemComplete += new EventHandler<LoaderEvtArgs>(onLoadItemComplete);
            request.load();
#endif




			//test only
			//ws.winx.platform.osx.OSXHIDInterface a = new ws.winx.platform.osx.OSXHIDInterface (new List<IDriver>());






        }


        void onUp(object o, EventArgs args)
        {
            Debug.Log("Up");
        }

        void onDown(object o, EventArgs args)
        {
            Debug.Log("Down");
        }

        void Handle1(object o, EventArgs args)
        {
            Debug.Log("Handle1");
        }

        void Handle2(object o, EventArgs args)
        {
            Debug.Log("Handle2");
        }

#if (UNITY_WEBPLAYER || UNITY_EDITOR || UNITY_ANDROID) && !UNITY_STANDALONE
        void onLoadComplete(object sender, LoaderEvtArgs args)
        {
           // Debug.Log(((List<WWW>)args.data).ElementAt(0).text);

            if (System.Threading.Thread.CurrentThread.ManagedThreadId != 1) return;


			//UnityEngine.Debug.Log("WebPlayer " + Path.Combine(Path.Combine(Application.dataPath, "StreamingAssets"), "InputSettings.xml"));
			
			

			UserInterfaceWindow ui=this.GetComponent<UserInterfaceWindow>();


            if (ui != null)//without settingsXML defined =>load them manually and attach them
            {
                InputManager.loadSettingsFromText(((List<WWW>)args.data).ElementAt(0).text);
                ui.StateInputCombinations = InputManager.Settings.stateInputs;
            }


            manuallyAddStateAndHandlers();
       
        }

        void onLoadItemComplete(object sender, LoaderEvtArgs args)
        {
           // Debug.Log(((WWW)args.data).text);
        }


        void onLoadError(object sender, LoaderEvtArgs args)
        {
             Debug.Log(((WWW)args.data).error);
        }
#endif




        void manuallyAddStateAndHandlers()
        {



            //   UnityEngine.Debug.Log(InputManager.Log());

            //		adding input-states pairs manually
            //			InputManager.MapStateToInput("My State1",new InputCombination(KeyCodeExtension.toCode(Joysticks.Joystick1,JoystickAxis.AxisPovX,JoystickPovPosition.Forward),(int)KeyCode.Joystick4Button9,(int)KeyCode.P,(int)KeyCode.JoystickButton0));
            //			InputManager.MapStateToInput("My State2",new InputCombination(KeyCode.Joystick4Button9,KeyCode.P,KeyCode.JoystickButton0));
            //			InputManager.MapStateToInput("My State3",new InputCombination("A(x2)+Mouse1+JoystickButton31"));
            //			InputManager.MapStateToInput("My State1",new InputCombination("Mouse1+Joystick12AxisXPositive(x2)+B"));



            ////easiest way to map state to combination (ex.of single W and C click)
            if (!InputManager.HasInputState("ManualAddedSTATE"))
                InputManager.MapStateToInput("ManualAddedSTATE", KeyCodeExtension.W.SINGLE, KeyCodeExtension.C.SINGLE);

            UnityEngine.Debug.Log("Log:" + InputManager.Log());


            ////Event Based input handling
            InputEvent ev = new InputEvent("ManualAddedSTATE");
            //InputEvent ev = new InputEvent((int)States.SomeState);

            ev.INPUT += new EventHandler(Handle1);
            ev.INPUT += new EventHandler(Handle2);
            ev.UP += new EventHandler(onUp);//this wouldn't fire for combo inputs(single only)
            ev.DOWN += new EventHandler(onDown);//this wouldn't fire for combo inputs(single only)

            _settingsLoaded = true;



        }


        // Update is called once per frame
        void Update()
        {
		


            //Use is mapping states so no quering keys during gameplay
            if (InputManager.EditMode || !_settingsLoaded) return;


            //Input.GetInput allows combos (combined input actions)
            if (InputManager.GetInputDown((int)States.Wave))// || InputManager.GetInput((int)States.Wave,true))
            // if (InputManager.GetInput((int)States.Wave,false))
            {
                Debug.Log("Wave Down");
                animator.Play((int)States.Wave);
            }


            if (InputManager.GetInputUp((int)States.MyCustomState))
            {
                Debug.Log(States.MyCustomState + "-Up");
                // animator.Play((int)States.Wave);
            }


            #region Testing Fuzer
            //Demo of use of M+ and Acc data from Wiimote
            //you will use ofcourse some sofisticate algo based on Kalman or Magdwick filter to process data


            //take all devices of type WiimoteDevice
            List<WiimoteDevice> wiimoteDevices = InputManager.GetJoysticks<WiimoteDevice>();

            if (wiimoteDevices.Count > 0)
            {
                WiimoteDevice device = wiimoteDevices[0];

                if (device.isReady)
                {
                    if (device.motionPlus != null && device.motionPlus.Enabled)
                    {
                        if (lastTime < 0f)
                        {
                            lastTime = Time.time;
                        }

                        complementaryFuzer.Update(device.Axis[JoystickAxis.AxisAccX].value, device.Axis[JoystickAxis.AxisAccY].value, device.Axis[JoystickAxis.AxisAccZ].value, device.motionPlus.Values.x * DEG_TO_RAD, device.motionPlus.Values.y * DEG_TO_RAD, device.motionPlus.Values.z * DEG_TO_RAD, Time.time - lastTime);
                        lastTime = Time.time;



                        Angles.x = (float)(complementaryFuzer.Angles.x * RAD_TO_DEG);
                        Angles.y = (float)(complementaryFuzer.Angles.z * RAD_TO_DEG);
                        Angles.z = (float)(complementaryFuzer.Angles.y * RAD_TO_DEG);

                        // UnityEngine.Debug.Log(Angles.z);
                       // UnityEngine.Debug.Log(Angles.x + " " + Angles.y + " " + Angles.z);

                        wiimote.transform.rotation = Quaternion.Euler(Angles);
                    }
                }
            }

            #endregion


            //if (InputManager.GetInput((int)States.MyCustomState, false))
            //{
            //    Debug.Log(States.MyCustomState + "-Hold");
            //    // animator.Play((int)States.Wave);
            //}


            //if (InputManager.GetInputUp((int)States.Wave))
            //{
            //    Debug.Log(States.Wave + "-Up");
            //    // animator.Play((int)States.Wave);
            //}



            //        if (InputManager.GetInput((int)States.Walk_Forward, false))
            //        {
            //
            //        }
            //	
            //
            //          if(InputManager.GetInputDown((int)States.Walk_Forward)){
            //				Debug.Log("Down");
            //			}
            //
            //			if(InputManager.GetInputUp((int)States.Walk_Forward)){
            //				Debug.Log("Up");
            //			}
            //
            ////		
            ////
            //          //using input as analog value
            //			float analogValue=InputManager.GetInput((int)States.Walk_Forward,false,0.3f,0.1f,0f);
            //			analogValue-=InputManager.GetInput((int)States.Base_Layer_MyState,false,0.3f,0.1f,0f);
            //
            ////			Debug.Log(analogValue);





        }




        void OnGUI()
        {


            //don't take device here in the loop this is just for demo

            if (InputManager.Devices.ContainsIndex(0))
                device = InputManager.Devices[(byte)0] as ThrustmasterRGTFFDDevice;

            if (device == null) return;

            //#if UNITY_ANDROID

            vSliderValue = GUI.HorizontalSlider(new Rect(25, 520, 400, 100), vSliderValue, 255.0F, 0.0F);
            // #endif

            if (vSliderValue != vSliderValuePrev)
                device.SetMotor(Convert.ToByte(vSliderValue), 0xA7, onMotorSet);

            vSliderValuePrev = vSliderValue;



            if (GUI.Button(new Rect(25, 590, 100, 130), "Stop Motor"))
            {
                //timer.Stop();
                StopCoroutine(runEffectEnumerator);
                device.StopMotor(onMotorStop);
                vSliderValue = 128;
            }

            if (GUI.Button(new Rect(150, 590, 100, 130), "Rumble"))
            {
                // timer.Stop();
                // this.InvokeRepeating("runEffect",1,
                runEffectEnumerator = runEffect();

                device.StopMotor(onMotorStop);

                StartCoroutine(runEffectEnumerator);
                // timer.Start();
            }
        }


        void onMotorStop(bool success)
        {
            Debug.Log("Motor stop was successful:" + success);
        }

        void onMotorSet(bool success)
        {
            Debug.Log("Motor command was successful:" + success);
        }

        IEnumerator runEffect()
        {
            while (true)
            {
                forceX += 0xA7;
                device.SetMotor(forceX, forceX, onMotorSet);

                yield return new WaitForSeconds(0.5f);
            }

            // yield break;

        }

        void onTimerElapsed(object sender, ElapsedEventArgs args)
        {
            forceX += 0xA7;
            device.SetMotor(forceX, forceX, onMotorSet);
        }







        /// <summary>
        /// DONT FORGET TO CLEAN AFTER YOURSELF
        /// </summary>
        void OnDestroy()
        {
            if (device != null)
                device.StopMotor();
            InputManager.Dispose();
        }
    }
}

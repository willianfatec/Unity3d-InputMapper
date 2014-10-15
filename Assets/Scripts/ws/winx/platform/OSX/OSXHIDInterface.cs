//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using System;
using System.Collections.Generic;
using ws.winx.devices;
using System.Runtime.InteropServices;
using System.Linq;


namespace ws.winx.platform.osx
{

   

    using UnityEngine; 
	using CFAllocatorRef=System.IntPtr;
	using CFDictionaryRef=System.IntPtr;
	using CFStringRef=System.IntPtr;
	using CFNumberRef=System.IntPtr;
	using CFArrayCallBacks=System.IntPtr;
	using CFArrayRef = System.IntPtr;
	using CFTypeRef = System.IntPtr;
	using IOHIDDeviceRef = System.IntPtr;
	using IOHIDElementRef = System.IntPtr;
	using IOHIDManagerRef = System.IntPtr;
	using IOHIDValueRef = System.IntPtr;
	using IOOptionBits = System.IntPtr;
	using IOReturn = Native.IOReturn;//System.IntPtr;
	using IOHIDElementCookie = System.UInt32;
	using CFTypeID=System.UInt64;
	using CFIndex =System.Int64;



    sealed class OSXHIDInterface : IHIDInterface,IDisposable
    {

#region Fields

        readonly IOHIDManagerRef hidmanager;


        readonly IntPtr RunLoop = Native.CFRunLoopGetMain();
		readonly IntPtr InputLoopMode = Native.RunLoopModeDefault;
        readonly Native.CFArray DeviceTypes;

        Native.IOHIDDeviceCallback HandleDeviceAdded;
        Native.IOHIDDeviceCallback HandleDeviceRemoved;
     

        bool disposed;


        private List<IDriver> __drivers = new List<IDriver>();



        private IDriver __defaultJoystickDriver;
	

      
		private Dictionary<int, HIDDevice> __Generics;
		
		
		public event EventHandler<DeviceEventArgs<int>> DeviceDisconnectEvent;
		
		public event EventHandler<DeviceEventArgs<IDevice>> DeviceConnectEvent;



        #endregion

#region IHIDInterface implementation

		public void Enumerate(){


			if (hidmanager != IntPtr.Zero) {	
				
				
				__Generics = new Dictionary<int, HIDDevice>();
				
				//Register add/remove device handlers
				RegisterHIDCallbacks(hidmanager);
				
				
			}else
				UnityEngine.Debug.LogError("Creating of OSX HIDManager failed");         

				}

		public HIDReport Read(int pid){
			return __Generics [pid].Read ();
				}

		public void Read (int pid, HIDDevice.ReadCallback callback)
		{
			__Generics [pid].Read (callback);
		}

		public void Read (int pid, HIDDevice.ReadCallback callback, int timeout)
		{
			throw new NotImplementedException ();
		}

		public void Write (object data, int pid, HIDDevice.WriteCallback callback, int timeout)
		{
			throw new NotImplementedException ();
		}

		public void Write (object data, int pid, HIDDevice.WriteCallback callback)
		{
			throw new NotImplementedException ();
		}

		public void Write (object data, int pid)
		{
			__Generics [pid].Write (data);
		}








		public Dictionary<int, HIDDevice> Generics {
			get {
				return __Generics;
			}
		}


        public IDriver defaultDriver
        {
            get { if (__defaultJoystickDriver == null) { __defaultJoystickDriver = new OSXDriver(); } return __defaultJoystickDriver; }
            set { __defaultJoystickDriver = value; }

        }




      





        public void Update()
        {
        }

        #endregion



#region Contsructor

        public OSXHIDInterface(List<IDriver> drivers)
        {
            __drivers = drivers;


            HandleDeviceAdded = DeviceAdded;
            HandleDeviceRemoved = DeviceRemoved;
          
            CFDictionaryRef[] dictionaries;
           


		

            
            dictionaries = new CFDictionaryRef[3];

            //create 3 search patterns by Joystick,GamePad and MulitAxisController

			// base.typeRef = CFLibrary.CFDictionaryCreate(IntPtr.Zero,keyz,values,keys.Length,ref kcall,ref vcall); 

		


			dictionaries[0] = CreateDeviceMatchingDictionary((uint)Native.HIDPage.GenericDesktop,(uint)Native.HIDUsageGD.Joystick);
			
			
			
			dictionaries[1] = CreateDeviceMatchingDictionary((uint)Native.HIDPage.GenericDesktop,(uint)Native.HIDUsageGD.GamePad);

			dictionaries[2] = CreateDeviceMatchingDictionary((uint)Native.HIDPage.GenericDesktop,(uint)Native.HIDUsageGD.MultiAxisController);





			
			DeviceTypes= new Native.CFArray (dictionaries);


            //create Hid manager	
            hidmanager = Native.IOHIDManagerCreate(IntPtr.Zero,(int)Native.IOHIDOptionsType.kIOHIDOptionsTypeNone);







        }
        #endregion





#region Private Members

		static CFDictionaryRef CreateDeviceMatchingDictionary(uint inUsagePage, uint inUsage)
		{
				IntPtr pageCFNumberRef = (new Native.CFNumber ((int)inUsagePage)).typeRef;
				IntPtr usageCFNumberRef = (new Native.CFNumber ((int)inUsage)).typeRef;
				CFStringRef[] keys;
				keys = new IntPtr[2];
				keys [0] = Native.CFSTR (Native.IOHIDDeviceUsagePageKey);//new Native.CFString(Native.IOHIDDeviceUsagePageKey);
				keys [1] = Native.CFSTR (Native.IOHIDDeviceUsageKey);//new Native.CFString(Native.IOHIDDeviceUsageKey);

				Native.CFDictionary dict = new Native.CFDictionary (keys, new IntPtr[] { 
				pageCFNumberRef,usageCFNumberRef});
			
				return dict.typeRef;
		}
			

		// Registers callbacks for device addition and removal. These callbacks
		// are called when we run the loop in CheckDevicesMode
		void RegisterHIDCallbacks(IOHIDManagerRef hidmanager)
		{
			try{
				UnityEngine.Debug.Log("OSXHIDInterface> RegisterHIDCallbacks");

				Native.IOHIDManagerRegisterDeviceMatchingCallback(
                hidmanager, HandleDeviceAdded, IntPtr.Zero);
            Native.IOHIDManagerRegisterDeviceRemovalCallback(
                hidmanager, HandleDeviceRemoved, IntPtr.Zero);
            Native.IOHIDManagerScheduleWithRunLoop(hidmanager,
                                                          RunLoop, InputLoopMode);

            //Native.IOHIDManagerSetDeviceMatching(hidmanager, DeviceTypes.Ref);
            Native.IOHIDManagerSetDeviceMatchingMultiple(hidmanager, DeviceTypes.typeRef);

				Native.CFRelease(DeviceTypes.typeRef);


            IOReturn result=Native.IOHIDManagerOpen(hidmanager, (int)Native.IOHIDOptionsType.kIOHIDOptionsTypeNone);

				if(result==IOReturn.kIOReturnSuccess){
					Native.CFRunLoopRunInMode(InputLoopMode, 0.0, true);
				}else{
					UnityEngine.Debug.LogError("OSXHIDInterface can't open hidmanager! Error:"+result);
				}

            

			}catch(Exception ex){
				UnityEngine.Debug.LogException(ex);
						}
           
        }


        /// <summary>
        /// Devices the added.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="res">Res.</param>
        /// <param name="sender">Sender.</param>
        /// <param name="device">Device.</param>
        void DeviceAdded(IntPtr context, IOReturn res, IntPtr sender, IOHIDDeviceRef device)
        {
			//IOReturn success = Native.IOHIDDeviceOpen (device, (int)Native.IOHIDOptionsType.kIOHIDOptionsTypeNone);

				int product_id = (int)(new Native.CFNumber(Native.IOHIDDeviceGetProperty(device, Native.CFSTR(Native.IOHIDProductIDKey)))).ToInteger();

				if(Generics.ContainsKey(product_id)) return;

				int vendor_id =(int)(new Native.CFNumber(Native.IOHIDDeviceGetProperty(device, Native.CFSTR(Native.IOHIDVendorIDKey)))).ToInteger();

				string description = (new Native.CFString(Native.IOHIDDeviceGetProperty(device, Native.CFSTR(Native.IOHIDProductKey)))).ToString();

				int location=(int)(new Native.CFNumber(Native.IOHIDDeviceGetProperty(device, Native.CFSTR(Native.IOHIDLocationIDKey)))).ToInteger();
				string transport=(new Native.CFString(Native.IOHIDDeviceGetProperty(device, Native.CFSTR(Native.IOHIDTransportKey)))).ToString();

				string path=String.Format("{0:s}_{1,4:X}_{2,4:X}_{3:X}",
				                          transport, vendor_id, product_id, location);//"%s_%04hx_%04hx_%x"
					
				

				GenericHIDDevice hidDevice;
				IDevice joyDevice = null;
               // IDevice<IAxisDetails, IButtonDetails, IDeviceExtension> joyDevice = null;


				///loop thru drivers and attach the driver to device if compatible
				if (__drivers != null)
					foreach (var driver in __drivers)
                {

					hidDevice=new GenericHIDDevice(__Generics.Count,vendor_id, product_id, device, this,path,description);

                    if ((joyDevice = driver.ResolveDevice(hidDevice)) != null)
					{

						this.DeviceConnectEvent(this,new DeviceEventArgs<IDevice>(joyDevice));

						__Generics[hidDevice.PID] = hidDevice;



						Debug.Log("Device PID:" + joyDevice.PID + " VID:" + joyDevice.VID + " attached to " + driver.GetType().ToString());

                        break;
                    }
                }

                if (joyDevice == null)
                {//set default driver as resolver if no custom driver match device

					hidDevice=new GenericHIDDevice(__Generics.Count,vendor_id, product_id, device, this,path,description);


					if ((joyDevice = defaultDriver.ResolveDevice(hidDevice)) != null)
                    {
                       
						this.DeviceConnectEvent(this,new DeviceEventArgs<IDevice>(joyDevice));
						__Generics[hidDevice.PID] = hidDevice;
						
						
                       
                    }
                        else
				    {
                        Debug.LogWarning("Device PID:" + product_id.ToString() + " VID:" + product_id.ToString() + " not found compatible driver on the system.Removed!");
                    
                    }


					Debug.Log("Device PID:" + joyDevice.PID + " VID:" + joyDevice.VID + " attached to " + defaultDriver.GetType().ToString());




                }



            
        }


        /// <summary>
        /// Devices the removed.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="res">Res.</param>
        /// <param name="sender">Sender.</param>
        /// <param name="device">Device.</param>
        void DeviceRemoved(IntPtr context, IOReturn res, IntPtr sender, IOHIDDeviceRef deviceRef)
        {
            if (Native.IOHIDDeviceConformsTo(deviceRef, Native.HIDPage.GenericDesktop, Native.HIDUsageGD.Joystick)
                 || Native.IOHIDDeviceConformsTo(deviceRef, Native.HIDPage.GenericDesktop, Native.HIDUsageGD.GamePad)
                 || Native.IOHIDDeviceConformsTo(deviceRef, Native.HIDPage.GenericDesktop, Native.HIDUsageGD.MultiAxisController)
            )
			{
				int product_id = (int)(new Native.CFNumber(Native.IOHIDDeviceGetProperty(deviceRef, Native.CFSTR(Native.IOHIDProductIDKey)))).ToInteger();


				if( !__Generics.ContainsKey(product_id)) return;

			UnityEngine.Debug.Log(String.Format("Joystick device {0:x} disconnected, sender is {1:x}", deviceRef, sender));
                
				this.DeviceDisconnectEvent(this,new DeviceEventArgs<int>(product_id));

				Generics.Remove(product_id);

				 Native.IOHIDDeviceUnscheduleFromRunLoop(deviceRef, RunLoop, InputLoopMode);
				Native.IOHIDDeviceRegisterInputValueCallback(deviceRef,IntPtr.Zero,IntPtr.Zero);
				
				
			}


		    
          
        }


        #endregion










       




        void IDisposable.Dispose()
        {
           if (hidmanager != IntPtr.Zero) {

				//throw exception don't know why
//				if(RunLoop!=IntPtr.Zero && InputLoopMode!=IntPtr.Zero)
//				Native.IOHIDDeviceUnscheduleWithRunLoop(hidmanager,
//				                                        RunLoop, InputLoopMode);
				Native.IOHIDManagerRegisterDeviceMatchingCallback(
					hidmanager, IntPtr.Zero, IntPtr.Zero);
				Native.IOHIDManagerRegisterDeviceRemovalCallback(
					hidmanager, IntPtr.Zero, IntPtr.Zero);

				Native.CFRelease(hidmanager);
			}



			if (Generics != null) {
								foreach (KeyValuePair<int, HIDDevice> entry in Generics) {
										entry.Value.Dispose ();
								}
			

								Generics.Clear ();
						}






				        if(__drivers!=null) __drivers.Clear();
        }
    }
}

#endif
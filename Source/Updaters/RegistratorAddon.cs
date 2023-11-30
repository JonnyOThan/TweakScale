using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale
{
	// this doesn't make much sense.  isn't this static bool _loadedInScene shared between all derived classes?
	// won't this break if more than one mod that uses this thing is installed?
	public abstract class RescalableRegistratorAddon : MonoBehaviour
	{
		private static bool _loadedInScene;

		public void Start()
		{
			if (_loadedInScene)
			{
				Destroy(gameObject);
				return;
			}
			_loadedInScene = true;
			OnStart();
		}

		public abstract void OnStart();

		public void Update()
		{
			_loadedInScene = false;
			Destroy(gameObject);
		}
	}
}

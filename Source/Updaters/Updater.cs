using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TweakScale
{
    // we should be able to get rid of this thing - the only client is the particle emitter and it could probably do all its work in OnRescale
    interface IUpdateable
    {
        void OnUpdate();
    }
}
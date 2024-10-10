using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Assets.KaomoLab.CSEmulator.Components;
using ClusterVR.CreatorKit.Preview.PlayerController;
using UnityEngine;


namespace Assets.KaomoLab.CSEmulator.Editor.Engine
{
    //Reflection以上のことをさせてはいけない。
    public class DesktopPlayerControllerReflector
        : Components.IVelocityYHolder,
        Components.IBaseMoveSpeedHolder,
        Components.IPlayerRotateHolder,
        Components.IRidingHolder
    {
        readonly DesktopPlayerController controller;

        readonly FieldInfo velocityY;
        readonly FieldInfo baseMoveSpeed;
        readonly FieldInfo rootTransform;
        readonly PropertyInfo isRiding;

        float IVelocityYHolder.value
        {
            get => (float)velocityY.GetValue(controller);
            set => velocityY.SetValue(controller, value);
        }
        float IBaseMoveSpeedHolder.value {
            get => (float)baseMoveSpeed.GetValue(controller);
            set => baseMoveSpeed.SetValue(controller, value);
        }

        Transform IPlayerRotateHolder.rotateTransform => (Transform)rootTransform.GetValue(controller);

        bool IRidingHolder.isRiding => (bool)isRiding.GetValue(controller);

        public DesktopPlayerControllerReflector(
            DesktopPlayerController controller
        )
        {
            this.controller = controller;
            velocityY = typeof(DesktopPlayerController)
                .GetField("velocityY", BindingFlags.NonPublic | BindingFlags.Instance);
            baseMoveSpeed = typeof(DesktopPlayerController)
                .GetField("baseMoveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
            rootTransform = typeof(DesktopPlayerController)
                .GetField("rootTransform", BindingFlags.NonPublic | BindingFlags.Instance);
            isRiding = typeof(DesktopPlayerController)
                .GetProperty("IsRiding", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}

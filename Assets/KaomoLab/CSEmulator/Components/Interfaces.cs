using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Components
{
    public interface IVelocityYHolder
    {
        public float value { get; set; }
    }
    public interface IBaseMoveSpeedHolder
    {
        public float value { get; set; }
    }
    public interface IPlayerRotateHolder
    {
        public Transform rotateTransform { get; }
    }
    public interface IRidingHolder
    {
        public bool isRiding { get; }
    }
    public interface IPerspectiveChangeNotifier
    {
        event Handler<bool> OnChanged;
        void RequestNotify();
    }
    public interface IPlayerMeasurementsHolder
    {
        public float height { get; }
        public float radius { get; }
    }
    public interface IGrabController
    {
        bool isGrab { get; }
        Vector3 grabPoint { get; }
        void ApplyUpdate();
    }
    public interface IPlayerFaceController
    {
        Transform vrmRotateRoot { get; }
        float GetNowRotate();
        void SetBaseRotate(float degree);
        void SetFaceForward(int direction);
        void SetFaceRight(int direction);
    }
    public interface IVrmIKNotifier
    {
        event Action<int> OnIK;
    }

    public interface IVariablesStore
    {
        IEnumerable<IVariable> GetVariables();
        event Action OnVariablesUpdated;
    }
    public interface IVariable
    {
        string name { get; }
        string value { get; }
        string type { get; }
        bool hasChild { get; }
        IEnumerable<IVariable> children { get; }
    }
}

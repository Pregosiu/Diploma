using Assets.Scripts.ClassStructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class DragAndDropManipulator : PointerManipulator
{
    public DragAndDropManipulator(VisualElement cont, int i, Dictionary<int, Player> playerDic, int lastExpanded, Label trainingLabel,
         bool slotted, TrainingSession ts, DateTime lstTimestamp)
    {       
        string obj = "object" + i;
        this.target = cont.Q<VisualElement>(obj);

        _trainingLabel = trainingLabel;
        _cont = cont;       
        root = cont;
        _playerDic = playerDic;
        _lastExpanded = lastExpanded;
        _slotted = slotted;
        _trainingSession = ts;
        if (lstTimestamp != DateTime.MinValue)
        {
            _lastTimestamp = lstTimestamp;
        }

    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(PointerDownHandler);
        target.RegisterCallback<PointerMoveEvent>(PointerMoveHandler);
        target.RegisterCallback<PointerUpEvent>(PointerUpHandler);
        target.RegisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(PointerDownHandler);
        target.UnregisterCallback<PointerMoveEvent>(PointerMoveHandler);
        target.UnregisterCallback<PointerUpEvent>(PointerUpHandler);
        target.UnregisterCallback<PointerCaptureOutEvent>(PointerCaptureOutHandler);
    }

    private Vector2 targetStartPosition { get; set; }
    private Label _trainingLabel;
    private VisualElement _cont;
    private int _lastExpanded;
    private int _trainingId;
    private Vector3 pointerStartPosition { get; set; }
    private Dictionary<int, Player> _playerDic;
    private bool _slotted;
    private DateTime _lastTimestamp;
    private VisualElement _targetCopy;
    private TrainingSession _trainingSession;

    private bool enabled { get; set; }

    private VisualElement root { get; }

    private void PointerDownHandler(PointerDownEvent evt)
    {
        targetStartPosition = target.transform.position;
        pointerStartPosition = evt.position;
        
        target.CapturePointer(evt.pointerId);
        enabled = true;
    }

    private void PointerMoveHandler(PointerMoveEvent evt)
    {
        if (enabled && target.HasPointerCapture(evt.pointerId))
        {
            Vector3 pointerDelta = evt.position - pointerStartPosition;

            target.transform.position = new Vector2(
                (targetStartPosition.x + pointerDelta.x),
                (targetStartPosition.y + pointerDelta.y));
        }
    }
    private void PointerUpHandler(PointerUpEvent evt)
    {
        if (enabled && target.HasPointerCapture(evt.pointerId))
        {
            target.ReleasePointer(evt.pointerId);
        }
    }
    private void PointerCaptureOutHandler(PointerCaptureOutEvent evt)
    {
        if (enabled)
        {
            //finds all the slots currently on the screen
            VisualElement slotsContainer = root.Q<VisualElement>("slots");

            UQueryBuilder<VisualElement> allSlots =
                root.Query<VisualElement>("slot");

            UQueryBuilder<VisualElement> allSlotsListQ =
                root.Query<VisualElement>("slot");
            
            List<VisualElement> allSlotsList = allSlotsListQ.ToList();

            UQueryBuilder<VisualElement> overlappingSlots =
                allSlots.Where(OverlapsTarget);
            VisualElement closestOverlappingSlot =
                FindClosestSlot(overlappingSlots);


            Vector3 closestPos = Vector3.zero;
            if (closestOverlappingSlot != null)
            {

                int closestSlotIndex = allSlotsList.IndexOf(closestOverlappingSlot);
                
                DateTime startDate = DatabaseManager.Instance.GameState.CurrentTime;

                int dayOfWeek = (int)(startDate.DayOfWeek);
                if (dayOfWeek == -1)
                {
                    dayOfWeek = 6;
                }

                //finds date that the closest slot represents
                DateTime dicIndex = startDate.AddDays(closestSlotIndex).AddDays(-1 * (dayOfWeek - 1));


                Player player = _playerDic.ElementAt(_lastExpanded).Value;


                // ------- Remove after tests -------
                /*if (_lastTimestamp > DatabaseManager.Instance.GameState.CurrentTime)
                {
                    target.transform.position = targetStartPosition;
                    return;
                }*/

                // checks if there is already training slotted for given date and returns training to previous position if true
                if (DatabaseManager.Instance.GameState.Players.Find(x => x == player).TrainingData.TrainingSchedule.ContainsKey(dicIndex))
                {

                    target.transform.position = targetStartPosition;
                    return;
                }

                // checks whether training is slotted into calendar and if it is removes training from that date if it has been moved
                if (_slotted == true)
                {
                    DatabaseManager.Instance.GameState.Players.Find(x => x == player).TrainingData.TrainingSchedule.Remove(_lastTimestamp);
                }


                closestPos = RootSpaceOfSlot(closestOverlappingSlot);
                closestPos = new Vector2(closestPos.x - target.layout.x, closestPos.y - target.layout.y);

                //adds the training to the player
                DatabaseManager.Instance.GameState.Players.Find(x => x == player).TrainingData.TrainingSchedule.Add(dicIndex, _trainingSession);

                _lastTimestamp = dicIndex;
                _slotted = true;
            }
            target.transform.position = closestOverlappingSlot != null ? closestPos : targetStartPosition;

            enabled = false;
        }
    }

    private bool OverlapsTarget(VisualElement slot)
    {
        return target.worldBound.Overlaps(slot.worldBound);
    }

    private VisualElement FindClosestSlot(UQueryBuilder<VisualElement> slots)
    {
        List<VisualElement> slotsList = slots.ToList();
        float bestDistanceSq = float.MaxValue;
        VisualElement closest = null;
        foreach (VisualElement slot in slotsList)
        {
            Vector3 displacement =
                RootSpaceOfSlot(slot) - target.transform.position;
            float distanceSq = displacement.sqrMagnitude;
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                closest = slot;
            }
        }
        return closest;
    }

    private Vector3 RootSpaceOfSlot(VisualElement slot)
    {
        Vector2 slotWorldSpace = slot.parent.LocalToWorld(slot.layout.position);
        return root.WorldToLocal(slotWorldSpace);
    }
}

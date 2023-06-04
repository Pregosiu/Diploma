using Assets.Scripts.ClassStructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TrainingScreen : VisualElement
{
    private LabeledListElement _lle;
    private List<Player> _allPlayers;
    private List<Player> _players;
    private Team _playerTeam;
    private List<VisualElement> _entries;
    private VisualElement _weekEntry;
    private VisualElement _lb;
    private List<VisualElement> _entriesExpand;
    private StyleSheet _styleSheet;
    private Dictionary<int, Player> _playerDic = new Dictionary<int, Player>();
    private int _lastExpanded;
    private VisualElement _dummyEntry;
    private ScrollView _playerContainer;
    private int _previousExpandedEntry = -1;
    private int trainingIndexer;
    private List<Label> _trainingLabelList = new();



    public TrainingScreen()
    {
        RegisterCallback<AttachToPanelEvent>(Enable);
    }

    private void Enable(AttachToPanelEvent evt)
    {
        this.Clear();
        _trainingLabelList.Clear();
        VisualTreeAsset template = Resources.Load<VisualTreeAsset>("trainingScreenNew");
        template.CloneTree(this);


        _lle = this.Q<LabeledListElement>();
        _lb = this.Q<VisualElement>("list-body");
        _allPlayers = new List<Player>(DatabaseManager.Instance.GameState.Players);
        _playerTeam = DatabaseManager.Instance.GameState.GetCurrentTeam();
        _players = new List<Player>(DatabaseManager.Instance.GameState.Players);
        _entries = new List<VisualElement>();
        _styleSheet = Resources.Load<StyleSheet>("LabelStyle");
        _playerContainer = this.Q<ScrollView>();

        

        CreateTrainings();
        CreateWeekEntry();
        FilterPlayers();
        LoadLabeledList();
    }


    /// <summary>
    /// Loads player list into entry
    /// </summary>
    private void LoadLabeledList()
    {
        _playerDic.Clear();

        if (_players.Count == 0)
        {
            _playerContainer.Clear();
            return;
        }



        for (int i = 0; i < _players.Count; i++)
        {
            _playerDic.Add(i, _players[i]);


            _dummyEntry = Resources.Load<VisualTreeAsset>("player-entry").CloneTree();
            _entries.Add(_dummyEntry);

            /*----- tworze label dla kazdego property <Player> i wyciagam dane z aktualnego w petli gracza ----- */

            Label name = new Label(_players[i].FirstName);
            name.name = "Name";

            Label surname = new Label(_players[i].LastName);
            surname.name = "Surname";

            Label team = new Label(_players[i].Team.TeamName);
            team.name = "Team";

            Label potential = new Label("4/5");
            potential.name = "Potential";
            potential.style.opacity = 0.5f;

            Label ability = new Label("2/5");
            ability.name = "Ability";

            string buttonName = "button" + i;

            int a = i;
            Button expand = new Button(() => Expand(a));



            expand.style.width = StyleKeyword.Auto;
            expand.style.height = StyleKeyword.Auto;
            expand.name = buttonName;
            expand.text = ">";
            expand.style.fontSize = 12;
            expand.style.unityFontStyleAndWeight = FontStyle.Bold;

            /*----- dodaje labele do entry -----*/

            _entries[i][0].Add(expand);
            _entries[i][0].Add(name);
            _entries[i][0].Add(surname);
            _entries[i][0].Add(team);
            _entries[i][0].Add(ability);
            _entries[i][0].Add(potential);

            /* Stylizuje kontenery labelów i labele */

            for (int j = 0; j < _entries[i].childCount; j++)
            {
                _entries[i].ElementAt(j).style.flexBasis = 1;
            }

            /* ----- stylizuje entry ----- */

            _entries[i].style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            _entries[i].style.justifyContent = new StyleEnum<Justify>(Justify.SpaceAround);
            _entries[i].style.alignItems = new StyleEnum<Align>(Align.Center);
            _entries[i].style.paddingLeft = 30;
            _entries[i].style.paddingRight = 30;
        }

        
        for(int i = 0; i < _entries.Count; i++)
        {
            _playerContainer.Add(_entries[i]);
            _playerContainer.style.justifyContent = new StyleEnum<Justify>(Justify.SpaceBetween);
        }


        _playerContainer.styleSheets.Add(_styleSheet);
    }


    /// <summary>
    /// Legacy filter
    /// </summary>
    private void FilterPlayers()
    {
        for (int i = _players.Count - 1; i >= 0; i--)
        {
            Player player = _players[i];
            if (player.Team != _playerTeam)
            {
                _players.Remove(player);
            }
        }
    }


    /// <summary>
    /// Creates visual element under the player that was expanded, loads trainings which player already has
    /// </summary>
    private void Expand(int i)
    {
        //needed for create trainings
        _lastExpanded = i;


        //List for every training in system, important to later pass to Drag&Drop so it knows which training it manipulates
        foreach (Label label in _trainingLabelList)
        {
            this.Remove(label);
        }
        _trainingLabelList.Clear();

        CreateTrainings();

        //Dictionary which holds the date that the given slot represents
        Dictionary<DateTime, VisualElement> _dateSlot = new Dictionary<DateTime, VisualElement>();


        // Creates Visual element which will pop up under the expanded player and inserts into entry list under the player which was expanded

        _entriesExpand = new List<VisualElement>(_entries);
        VisualElement expandedEntry = Resources.Load<VisualTreeAsset>("player-entry-expand").CloneTree();
        expandedEntry[0].Add(new CalendarWidgetTrain());
        expandedEntry[0].style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
        expandedEntry[0].style.alignItems = new StyleEnum<Align>(Align.Stretch);
        expandedEntry.style.width = 950;
        expandedEntry.style.alignSelf = new StyleEnum<Align>(Align.Center);
        expandedEntry[0].style.marginTop = 0;
        expandedEntry[0].style.borderTopLeftRadius = 0;
        expandedEntry[0].style.borderTopRightRadius = 0;

        _entriesExpand.Insert(i + 1, expandedEntry);


        // Responsible for recovering margin for the entry which was expanded
        if (_previousExpandedEntry > i)
        {
            _previousExpandedEntry++;
        }
        if(_previousExpandedEntry != -1)
        {
            _entriesExpand[_previousExpandedEntry][0].style.marginBottom = 15;
        }
        _entriesExpand[i][0].style.marginBottom = 0;


        // Clears scrollview and loads list with expanded entry
        _playerContainer.Clear();
        for (int j = 0; j < _entriesExpand.Count; j++)
        {
            _playerContainer.Add(_entriesExpand[j]);
        }


        // finds all the slots in the expanded entry and forwards them to list
        UQueryBuilder<VisualElement> allSlots =
            this.Query<VisualElement>("slot");

        List<VisualElement> slotsList = allSlots.ToList();



        DateTime currentDate = DatabaseManager.Instance.GameState.CurrentTime;

        // returns number for day of the week, however monday is 1 and sunday is 0, our calendar starts from monday so have to make sunday a 6
        int dayOfWeek = (int)(currentDate.DayOfWeek);
        if (dayOfWeek == -1)
        {
            dayOfWeek = 6;
        }


        //calculates start date of calendar visual element
        DateTime startDate = currentDate.AddDays(-1 * (dayOfWeek - 1));

        //assigns date to each slot
        for (int z = 0; z < slotsList.Count; z++)
        {         
            _dateSlot.Add(startDate.AddDays(z), slotsList[z]);            
        }

        //important for distinct manipulators
        int tempindexer = trainingIndexer + 1;


        //Creates training labels for every training player has inside the calendar
        for (int z = 0; z < _players[i].TrainingData.TrainingSchedule.Count; z++)
        {


            Label trainingLabel = new Label();

            DateTime slotPosition = _players[i].TrainingData.TrainingSchedule.ElementAt(z).Key;

            int dayDiff = (int)(slotPosition.DayOfWeek) - (int)(startDate.DayOfWeek);
            if(dayDiff == -1)
            {
                dayDiff = 6;
            }


            //sets settings for trainings and slots them into calendar
            SetTrainingLabelSettings(trainingLabel);

            Vector2 slotPos = _dateSlot[slotPosition].parent.LocalToWorld(_dateSlot[slotPosition].layout.position);
           
            trainingLabel.style.left = slotPos.x - 198 + (125*dayDiff);
            trainingLabel.style.top = slotPos.y + 123 + (180 * i);
            trainingLabel.text = _players[i].TrainingData.TrainingSchedule[slotPosition].Name;
            trainingLabel.name = "object" + tempindexer;
            this.Add(trainingLabel);
            _trainingLabelList.Add(trainingLabel);

            TrainingSession ts = TrainingController.TrainingTypes.Find(x => x.Name == trainingLabel.text);

            DragAndDropManipulator manipulator = new(this, tempindexer, _playerDic, i, trainingLabel, true, ts, slotPosition);

            tempindexer++;
           
        }
        _previousExpandedEntry = i;
    }


    /// <summary>
    /// Legacy training calendar
    /// </summary>
    private void CreateWeekEntry()
    {
        _weekEntry = new VisualElement();
        _weekEntry.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
        _weekEntry.style.justifyContent = new StyleEnum<Justify>(Justify.FlexStart);
        _weekEntry.style.alignItems = new StyleEnum<Align>(Align.Center);
        _weekEntry.style.paddingLeft = 30;
        _weekEntry.style.paddingRight = 30;

        /*  Tworze 7 kontenerow na dni tygodnia, potem wsadzam do nich label i element do drag&drop  */

        Label mon = new Label("Mon");
        mon.name = "Mon";
        Label tue = new Label("Tue");
        tue.name = "Tue";
        Label wed = new Label("Wed");
        wed.name = "Wed";
        Label thu = new Label("Thu");
        thu.name = "Thu";
        Label fri = new Label("Fri");
        fri.name = "Fri";
        Label sat = new Label("Sat");
        sat.name = "Sat";
        Label sun = new Label("Sun");
        sun.name = "Sun";

        VisualElement monVis = new VisualElement();
        monVis.name = "slot";
        monVis.AddToClassList("slot");
        VisualElement tueVis = new VisualElement();
        tueVis.name = "slot";
        tueVis.AddToClassList("slot");
        VisualElement wedVis = new VisualElement();
        wedVis.name = "slot";
        wedVis.AddToClassList("slot");
        VisualElement thuVis = new VisualElement();
        thuVis.name = "slot";
        thuVis.AddToClassList("slot");
        VisualElement friVis = new VisualElement();
        friVis.name = "slot";
        friVis.AddToClassList("slot");
        VisualElement satVis = new VisualElement();
        satVis.name = "slot";
        satVis.AddToClassList("slot");
        VisualElement sunVis = new VisualElement();
        sunVis.name = "slot";
        sunVis.AddToClassList("slot");

        VisualElement monCon = new VisualElement();
        monCon.name = "MonCon";
        VisualElement tueCon = new VisualElement();
        tueCon.name = "TueCon";
        VisualElement wedCon = new VisualElement();
        wedCon.name = "WedCon";
        VisualElement thuCon = new VisualElement();
        thuCon.name = "ThuCon";
        VisualElement friCon = new VisualElement();
        friCon.name = "FriCon";
        VisualElement satCon = new VisualElement();
        satCon.name = "SatCon";
        VisualElement sunCon = new VisualElement();
        sunCon.name = "SunCon";

        monCon.Add(mon);
        monCon.Add(monVis);
        tueCon.Add(tue);
        tueCon.Add(tueVis);
        wedCon.Add(wed);
        wedCon.Add(wedVis);
        thuCon.Add(thu);
        thuCon.Add(thuVis);
        friCon.Add(fri);
        friCon.Add(friVis);
        satCon.Add(sat);
        satCon.Add(satVis);
        sunCon.Add(sun);
        sunCon.Add(sunVis);

        _weekEntry.Add(monCon);
        _weekEntry.Add(tueCon);
        _weekEntry.Add(wedCon);
        _weekEntry.Add(thuCon);
        _weekEntry.Add(friCon);
        _weekEntry.Add(satCon);
        _weekEntry.Add(sunCon);


        for (int j = 0; j < _weekEntry.childCount; j++)
        {

            /*_entries[i].ElementAt(j).style.flexBasis = 1;*/

            _weekEntry.ElementAt(j).style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);

            _weekEntry.ElementAt(j).ElementAt(1).style.borderBottomWidth = 1;
            _weekEntry.ElementAt(j).ElementAt(1).style.borderBottomColor = Color.black;

            _weekEntry.ElementAt(j).ElementAt(1).style.borderTopWidth = 1;
            _weekEntry.ElementAt(j).ElementAt(1).style.borderTopColor = Color.black;

            _weekEntry.ElementAt(j).ElementAt(1).style.borderLeftWidth = 1;
            _weekEntry.ElementAt(j).ElementAt(1).style.borderLeftColor = Color.black;

            _weekEntry.ElementAt(j).ElementAt(1).style.borderRightWidth = 1;
            _weekEntry.ElementAt(j).ElementAt(1).style.borderRightColor = Color.black;

            _weekEntry.ElementAt(j).ElementAt(1).style.width = 150;
            _weekEntry.ElementAt(j).ElementAt(1).style.height = 60;
            _weekEntry.ElementAt(j).ElementAt(1).style.flexGrow = 1;
        }
    }


    /// <summary>
    /// Creates label for every training in database 'l' times and created Drag&Drop manipulators for them
    /// </summary>
    private void CreateTrainings()
    {

        VisualElement cont = this.Q<VisualElement>("container");
     
        trainingIndexer = 0;

        for(int l = 0; l < 5; l++)
        {
            int i = 0;
            foreach (TrainingSession training in TrainingController.TrainingTypes)
            {
                Label trainingLabel = new Label();


                SetTrainingLabelSettings(trainingLabel);
                if (i % 2 == 0)
                {
                    trainingLabel.style.left = 90;
                }
                else
                {
                    trainingLabel.style.left = 260;
                }

                trainingLabel.style.top = 100 + 125 * (i / 2);
                trainingLabel.text = training.Name;
                TrainingSession ts = TrainingController.TrainingTypes.Find(x => x.Name == trainingLabel.text);


                trainingLabel.name = "object" + trainingIndexer;
                this.Add(trainingLabel);
                _trainingLabelList.Add(trainingLabel);

                DragAndDropManipulator manipulator = new(this, trainingIndexer, _playerDic, _lastExpanded, trainingLabel, false, ts, DateTime.MinValue);

                i++;

                //important for making distinct drag&drop manipulators
                trainingIndexer++;
            }
        }

    }

    /// <summary>
    /// Style changes to training label that are constant
    /// </summary>
    private void SetTrainingLabelSettings(Label trainingLabel)
    {
        trainingLabel.style.position = Position.Absolute;
        trainingLabel.style.backgroundColor = Color.black;
        trainingLabel.style.width = 125;
        trainingLabel.style.height = 100;
        trainingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        trainingLabel.styleSheets.Add(_styleSheet);
        trainingLabel.style.whiteSpace = WhiteSpace.Normal;
    }
}

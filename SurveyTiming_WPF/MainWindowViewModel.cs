using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ITC_Services;
using ITCLib;
using Microsoft.Extensions.DependencyInjection;
using MvvmLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace SurveyTiming_WPF
{
    // DONE: timing report
    // TODO: country timing report
    // DONE: missing weights
    // TODO: show prep vars
    // TODO: show pstp vars
    // TODO: find filter (with full question or at least prep)
    // DONE: import weights from word
    // DONE: time each question for list
    // DONE: go to var box
    // TODO: show variable info in preview pane (weight, source, time etc)
    // DONE: add word count, remove raw seconds, 
    // TODO: frequency code generation
    // TODO: color code list items based on question type

    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ISurveyService _surveyService;

        [ObservableProperty]
        private Survey currentSurvey;

        [ObservableProperty]
        [NotifyPropertyChangedFor("WeightedQuestions")]
        [NotifyPropertyChangedFor("MissingWeights")]
        private ObservableCollection<LinkedQuestion> questionList = new ObservableCollection<LinkedQuestion>();

        [ObservableProperty]
        private ObservableCollection<LinkedQuestion> selectedQuestions = new ObservableCollection<LinkedQuestion>();

        [ObservableProperty]
        private LinkedQuestion selectedQuestion;

        public string CurrentQuestionText => SelectedQuestion != null ? SelectedQuestion.GetQuestionTextHTML() : string.Empty;


        [ObservableProperty]
        private int wPM = 225;

        public string TotalTime => GetTiming(questionList);
        public string SelectedTotalTime => GetTiming(selectedQuestions);

        [ObservableProperty]
        private string importStatus = string.Empty;

        
        public List<Survey> AllSurveys { get; set; } = new List<Survey>();

        public IEnumerable<LinkedQuestion> WeightedQuestions => QuestionList?.Where(x => x?.Weight?.Value >= 0) ?? Enumerable.Empty<LinkedQuestion>();
        public IEnumerable<LinkedQuestion> MissingWeights => QuestionList?.Where(x => x?.Weight?.Value == -1) ?? Enumerable.Empty<LinkedQuestion>();

        private string timingFolder = @"\\psychfile\psych$\psych-lab-gfong\SMG\SDI\Survey Timing";
        private string countryFolder => Path.Combine(new string[] { timingFolder, CurrentSurvey.SurveyCodePrefix + CurrentSurvey.Wave.ToString(), "method 3"});

        public MainWindowViewModel(IServiceProvider services) 
        { 
            DisplayName = "Survey Timing - Weighted Questions";

            _surveyService = services.GetRequiredService<ISurveyService>();
            AllSurveys = _surveyService.GetAllSurveys().OrderBy(s => s.SurveyCode).ToList();
        }

        partial void OnCurrentSurveyChanged(Survey? oldValue, Survey newValue)
        {
            ImportStatus = string.Empty;
            if (newValue != null) 
                LoadSurvey(newValue.SID);
        }

        partial void OnSelectedQuestionChanged(LinkedQuestion? oldValue, LinkedQuestion newValue)
        {
            OnPropertyChanged(nameof(CurrentQuestionText));
        }

        [RelayCommand]
        private void NewTiming()
        {
            CurrentSurvey = null;
            ImportStatus = string.Empty;
            QuestionList = null;
            SelectedQuestion = null;
            WPM = 225;
            
        }

        [RelayCommand]
        private void LoadSurvey(int surveyId)
        {
            try
            {
                
                if (CurrentSurvey != null)
                {
                    var questions = _surveyService.GetQuestionsForSurvey(surveyId);
                    var linkedQuestions = questions.Select(q => new LinkedQuestion(q)).ToList();
                    QuestionList = new ObservableCollection<LinkedQuestion>(linkedQuestions);
                    SetAutomaticWeights();
                    ImportWeights(Path.Combine(countryFolder, CurrentSurvey.SurveyCode + "-stage2.txt"));
                    ImportWeights(Path.Combine(countryFolder, CurrentSurvey.SurveyCode + "-stage3.txt"));
                    ImportWeights(Path.Combine(countryFolder, CurrentSurvey.SurveyCode + "-stage4.txt"));
                    linkedQuestions.ForEach(q => q.Seconds= Math.Round(q.GetTiming(WPM),2));
                    OnPropertyChanged(nameof(WeightedQuestions));
                    OnPropertyChanged(nameof(MissingWeights));
                }
                else
                {
                    QuestionList.Clear();
                }
            }
            catch
            {
                
            }
        }

        private async Task LoadSurveyAsync(int surveyId)
        {
            try
            {

                if (CurrentSurvey != null)
                {
                    var questions = await _surveyService.GetQuestionsForSurveyAsync(surveyId);
                    var linkedQuestions = questions.Select(q => new LinkedQuestion(q)).ToList();
                    QuestionList = new ObservableCollection<LinkedQuestion>(linkedQuestions);
                    SetAutomaticWeights();
                    ImportWeights(Path.Combine(countryFolder, CurrentSurvey.SurveyCode + "-stage2.txt"));
                    ImportWeights(Path.Combine(countryFolder, CurrentSurvey.SurveyCode + "-stage3.txt"));
                    ImportWeights(Path.Combine(countryFolder, CurrentSurvey.SurveyCode + "-stage4.txt"));
                    linkedQuestions.ForEach(q => q.Seconds = Math.Round(q.GetTiming(WPM), 2));
                    OnPropertyChanged(nameof(WeightedQuestions));
                    OnPropertyChanged(nameof(MissingWeights));
                }
                else
                {
                    QuestionList.Clear();
                }
            }
            catch
            {

            }
        }

        [RelayCommand]
        private void CalculateTiming()
        {
            OnPropertyChanged(nameof(TotalTime));
        }

        [RelayCommand]
        private void RefreshQuestions()
        {
            if (CurrentSurvey != null)
            {
                var questions = _surveyService.GetQuestionsForSurvey(CurrentSurvey.SID);
                var linkedQuestions = questions.Select(q => new LinkedQuestion(q)).ToList();
                QuestionList = new ObservableCollection<LinkedQuestion>(linkedQuestions);
            }
            else
            {
                QuestionList.Clear();
            }
        }

        [RelayCommand]
        private void GenerateMissingWeightsReport()
        {
            TimingReports.MissingWeightsReport report = new TimingReports.MissingWeightsReport();
            string filename = $"{CurrentSurvey.SurveyCode} - Missing Weights Report";
            report.CreateReport(MissingWeights, filename);
        }

        [RelayCommand]
        private void GenerateTimingReport()
        {
            TimingReports.TimingReport report = new TimingReports.TimingReport();
            report.WPM = this.WPM;
            string filename = $"{CurrentSurvey.SurveyCode} - Timing Report";
            report.CreateReport(QuestionList, filename);
        }

        [RelayCommand]
        private void GenerateCountryReport()
        {
            TimingReports.CountryTimingReport report = new TimingReports.CountryTimingReport();
            string filename = string.Empty;
            report.CreateReport(QuestionList, filename);
        }

        [RelayCommand]
        private void ImportWeights(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            int count = WeightsImporter.ImportWeights(QuestionList.ToList(), filePath);

            ImportStatus += count + " weights imported from " + Path.GetFileName(filePath) + "\r\n"; 
        }

        [RelayCommand]
        private void ImportWeightsFromWord()
        {
            string filePath = "";// DialogService.ShowOpenFileDialog("Word Documents|*.docx;*.doc", "Select Word file with weights");

            if (string.IsNullOrEmpty(filePath))
                return;

            int count = WeightsImporter.ImportWeightsFromWord(QuestionList.ToList(), filePath);
            ImportStatus += count + " weights imported from " + filePath.Substring(filePath.LastIndexOf("\\") + 1) + "\r\n"; ;
        }

        [RelayCommand]
        private void CreateFreqCode()
        {
            
            if (CurrentSurvey.Wave <= 1) {
                ImportStatus += "Cannot find previous wave for this survey.";
                return;
            }

            // TODO get previous wave from user or determine
            //Survey 

            string sas = ""; //GenerateSASCodeResponseFreq(CurrentSurvey, frm.Surv)

            
            if (string.IsNullOrEmpty(sas))
                return;
            Directory.CreateDirectory(this.timingFolder + "\\Code");

            string filename = timingFolder + "\\Code\\" + CurrentSurvey.SurveyCode + "-ResponseFrequencyCode.txt";
            File.WriteAllText(filename, sas);

            System.Diagnostics.Process.Start(filename);
        }

        private string GetTiming(IEnumerable<LinkedQuestion> questions)
        {
            if (questions == null || questions.Count() == 0 || WPM <= 0)
            {
                return "N/A";
            }
            SurveyTiming surveyTimer = new SurveyTiming();

            double totalSeconds = questions.Sum(q => (q.GetTiming(WPM, true, false) * q.Weight.Value));
            
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return string.Format("{0} min {1} sec", (int)timeSpan.Minutes, timeSpan.Seconds);
        }

        
        private void SetAutomaticWeights()
        {
            if (QuestionList == null || QuestionList.Count == 0)
                return;

            int count1 = 0;
            int count0 = 0;

            foreach (LinkedQuestion lq in QuestionList)
            {
                if (lq.VarName.RefVarName.StartsWith("Z"))
                {
                    lq.Weight.Value = 0;
                    lq.Weight.Source = "A";
                    count0++;
                }
                else if (IsOtherSpecify(QuestionList, lq))
                {
                    lq.Weight.Value = 0;
                    lq.Weight.Source = "A";
                    count1++;
                }
                else if (lq.PrePW.WordingText.StartsWith("Ask all.") || lq.PrePW.WordingText.Contains("Ask all."))
                {
                    lq.Weight.Source = "A";
                    lq.Weight.Value = 1;
                    count1++;
                }
                else if (lq.IsProgramming() || lq.IsDerived() || lq.IsTermination())
                {
                    lq.Weight.Value = 0;
                    lq.Weight.Source = "A";
                    count0++;
                }
            }

            ImportStatus += count0 + " questions assigned weight of 0.\r\n";
            ImportStatus += count1 + " questions assigned weight of 1.\r\n";
        }

        private bool IsOtherSpecify(IEnumerable<SurveyQuestion> sourceList, SurveyQuestion q)
        {
            string varname = q.VarName.RefVarName;
            if (varname.EndsWith("o"))
            {
                var nonO = sourceList.Where(x => x.VarName.RefVarName.Equals(varname.Substring(0, varname.Length - 1)));

                if (nonO.Count() > 0)
                    return true;

            }

            return false;
        }

        private string GenerateSASCodeResponseFreq(Survey survey, Survey previousWave) //string project, string projectWave)
        {
            StringBuilder s = new StringBuilder();

            string cc = survey.CountryCode;
            string wave = Math.Floor(survey.Wave).ToString();
            int waveChar = 96 + int.Parse(wave) - 1;
            char waveLetter = (char)waveChar;

            // get previous wave
            List<SurveyQuestion> previousWaveQs = _surveyService.GetQuestionsForSurvey(previousWave.SID).ToList();

            foreach (LinkedQuestion q in QuestionList)
            {
                // skip those with weights
                if (q.Weight.Value > -1)
                    continue;
                // skip those without filters
                if (q.FilterList.Count == 0)
                    continue;

                if (!AllVarsPresent(q.FilteredOn, previousWaveQs))
                    continue;

                s.Append("%filterStat(newVar = " + q.VarName.VarName + ", filter = (");
                // GET EACH FILTER LIST
                foreach (List<FilterInstruction> fl in q.FilterList)
                {
                    s.Append("(");
                    foreach (FilterInstruction fi in fl)
                    {

                        string fullVarName = waveLetter + Utilities.ChangeCC(fi.VarName, cc);
                        if (fi.Range)
                            s.Append(fullVarName + " in (" + fi.ValuesStr[0] + ":" + fi.Values.Last() + ") AND ");
                        else if (fi.ValuesStr.Count > 1)
                        {
                            s.Append(fullVarName + " in (" + string.Join(", ", fi.ValuesStr) + ") AND ");
                        }
                        else
                        {
                            string op = "=";
                            if (fi.Oper == Operation.GreaterThan)
                                op = ">";
                            else if (fi.Oper == Operation.LessThan)
                                op = "<";
                            else if (fi.Oper == Operation.NotEquals)
                                op = "<>";

                            s.Append(fullVarName + op + fi.ValuesStr[0] + ") AND ");
                        }

                    }
                    s.Length--;
                    s.Length--;
                    s.Length--;
                    s.Length--;

                    s.Append(") OR ");
                }
                s.Length--;
                s.Length--; s.Length--;
                s.Length--;
                s.AppendLine(");");
            }

            return s.ToString();
        }

        private bool AllVarsPresent(List<LinkedQuestion> filterVars, List<SurveyQuestion> refList)
        {
            bool allFound = false;

            foreach (LinkedQuestion lq in filterVars)
            {
                if (!refList.Exists(x => x.VarName.RefVarName.Equals(lq.VarName.RefVarName)))
                    return allFound;
            }
            allFound = true;
            return allFound;
        }
    }
}

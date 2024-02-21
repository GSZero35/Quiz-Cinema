using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TowerDefense;
using SpaceShooter;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace QuizCinema
{
    public class UIManager : MonoBehaviour, IDependency<Score>, IDependency<AnswerData>
    {
        public event Action<Question> OnCreateAnswers;
        public event Action<List<AnswerData>> OnCorrectAnswer;
        public event Action<AnswerData> OnCorrectSpriteActivate;

        public enum ResolutionScreenType { Correct, Incorrect, Finish }

        [Header("References")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private QuestionMethods _questionMethods;
        [SerializeField] private Score _score;

        [SerializeField] SettingUIManager _settingUIManager;

        [Header("UI Elements (Prefabs)")]
        [SerializeField] private AnswerData[] _answerPrefab;

        [SerializeField] private UIElements _uIElements;

        private List<int> _finishedAnswers = new List<int>();
        public List<int> FinishedAnswers => _finishedAnswers;

        private List<AnswerData> _currentAnswer = new List<AnswerData>();
        private List<AnswerData> _correctAnswer = new List<AnswerData>();
        private int _resStateParaHash = 0;

        private IEnumerator IE_DisplayTimedResolution;

        private ResolutionScreenType _typeAnswer;

        public void Construct(Score obj) => _score = obj;
        public void Construct(AnswerData obj) 
        {
            var index = obj.AnswerIndex;
            _answerPrefab[index] = obj;   
        }



        private void OnEnable()
        {
            _questionMethods.UpdateQuestionUI += UpdateQuestionUI;
            _gameManager.UpdateDisplayScreenResolution += DisplayResolution;
            _score.UpdateScore += UpdateScoreUI;

        }

        private void OnDisable()
        {
            _questionMethods.UpdateQuestionUI -= UpdateQuestionUI;
            _gameManager.UpdateDisplayScreenResolution -= DisplayResolution;
            _score.UpdateScore -= UpdateScoreUI;
        }

        private void Awake()
        {
           // _uIElements = _settingUIManager.UIGameElements;
           
        }


        private void Start()
        {
            //UpdateScoreUI();
            _resStateParaHash = Animator.StringToHash("ScreenState");

            _answerPrefab = _settingUIManager.AnswersPrefabs;
          //  for (int i = 0; i < _questionInfoTextObject.Length; i++)
          //  {
          ///     _questionInfoTextObject[i] = _settingUIManager.QuestionInfoTextObject[i].GetComponentInChildren<TextMeshProUGUI>();
          //  }
        }


        private void UpdateQuestionUI(Question question)
        {
            UpdateScoreUI();

            var index = question.IndexPrefab;

            for (int i = 0; i < _answerPrefab.Length; i++)
            {
                _uIElements.QuestionInfoTextObject[i].transform.parent.gameObject.SetActive(false);
                _uIElements.CadrCinema[i].transform.parent.gameObject.SetActive(false);
                _uIElements.AnswerContentArea[i].transform.parent.parent.gameObject.SetActive(false);
            }

            ActivateUIObjects(index, question);

            if (!_questionMethods.IsFinished)
            {
                CreateAnswers(question);
            }
        }

        private void ActivateUIObjects(int index, Question question)
        {
            _uIElements.QuestionInfoTextObject[index].transform.parent.gameObject.SetActive(true);
            _uIElements.CadrCinema[index].transform.parent.gameObject.SetActive(true);
            _uIElements.AnswerContentArea[index].transform.parent.parent.gameObject.SetActive(true);
            _uIElements.QuestionInfoTextObject[index].text = question.Info;

            Sprite sprite = Resources.Load($"{question._cadrCinemaName}", typeof(Sprite)) as Sprite;
            _uIElements.CadrCinema[index].sprite = sprite;
        }

        private void DisplayResolution(ResolutionScreenType type, int score)
        {
            UpdateResUI(type, score);
            _uIElements.ResolutionScreenAnimator.SetInteger(_resStateParaHash, 2);
            _typeAnswer = type;
        }

        public void CloseResultQuestion()
        {
            if (_typeAnswer != ResolutionScreenType.Finish)
            {
                if (IE_DisplayTimedResolution != null)
                {
                    StopCoroutine(IE_DisplayTimedResolution);
                }
                IE_DisplayTimedResolution = DisplayTimedResolution();
                StartCoroutine(IE_DisplayTimedResolution);  
            }

            if (_typeAnswer == ResolutionScreenType.Finish)
            {
                _uIElements.FinishUIElements.gameObject.SetActive(true);
            }

        }

        private IEnumerator DisplayTimedResolution()
        {
            yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);

            Debug.Log("DisplayTimedResolution");
            _uIElements.ResolutionScreenAnimator.SetInteger(_resStateParaHash, 1);
        }

        private void UpdateResUI(ResolutionScreenType type, int score)
        {
            var currentEpisode = LevelSequenceController.Instance.CurrentEpisode;
            var sceneName = SceneManager.GetActiveScene().name;

            _uIElements.CountCurrentAnswer.text = _gameManager.CountCurrenttAnswer + "/" + _questionMethods.Data.Questions.Length;

            switch (type)
            {
                case ResolutionScreenType.Correct:
                    _uIElements.ResolutionStateInfoText.text = "Correct!";
                    _uIElements.ResolutionScoreText.text = "+" + score;
                    break;
                case ResolutionScreenType.Incorrect:
                    _uIElements.ResolutionStateInfoText.text = "Wrong!";
                    _uIElements.ResolutionScoreText.text = "-" + score;
                    break;
                case ResolutionScreenType.Finish:
                    _uIElements.ResolutionStateInfoText.text = "Final Score!";

                    StartCoroutine(CalculateScore());
                    //_uIElements.FinishUIElements.gameObject.SetActive(true); // 
                    UpdateFinishScreen(sceneName);

                    break;
            }
        }

        private void UpdateFinishScreen(string sceneName)
        {
            _uIElements.HighScoreText.gameObject.SetActive(true);
            _uIElements.HighScoreText.text = MapCompletion.Instance.GetLvlScore(sceneName).ToString();
            _uIElements.CountCorrectAnswer.text = "���-�� ���������� �������: " + _gameManager.CountCorrectAnswer + " \n���-�� ������������ �������: "
                + (_questionMethods.GetFinishedLengthQuestions - _gameManager.CountCorrectAnswer);

            var numberLvl = MapCompletion.Instance.GetLvlNumber(sceneName) + 1; // �.� ��������� � 0 (� ������ 1 ������ 0)
            _uIElements.TextFinalLvl.text = $"������� {numberLvl}";

            if (MapCompletion.Instance.GetLvlScore(sceneName) > 1)
            {
                _uIElements.EnableButtonFinishNextLvl.SetActive(true);
                _uIElements.TextSuccessLvl.text = "�������";
            }
            else
            {
                _uIElements.EnableButtonFinishReloadLvl.SetActive(true);
                _uIElements.TextSuccessLvl.text = "�� �������";
            }
        }

        IEnumerator CalculateScore()
        {
            if (_score.CurrentLvlScore == 0)
            {
                _uIElements.ScoreFinalLvl.text = 0.ToString();
                yield break;
            }

            var scoreValue = 0;
            var scoreMoreThanZero = _score.CurrentLvlScore > 0;

            while (scoreMoreThanZero ?  scoreValue < _score.CurrentLvlScore : scoreValue > _score.CurrentLvlScore)
            {
                yield return new WaitForSeconds(0.001f);
                scoreValue += scoreMoreThanZero ? 1 : -1;
                _uIElements.ScoreFinalLvl.text = scoreValue.ToString();

                yield return null;
            }
        }

        private void CreateAnswers(Question question)
        {
            _correctAnswer.Clear();

            EraseAnswers();

            var index = question.IndexPrefab;

            var listIndexCorrectAnswer = question.GetCorrectAnswers();

            UpdateCorrectAnswerList(question);

            OnCorrectAnswer?.Invoke(_correctAnswer);

            OnCreateAnswers?.Invoke(question);
        }

        public static void Shuffle<T>(T[] arr)
        {
            System.Random rand = new System.Random();

            for (int i = arr.Length - 1; i >= 1; i--)
            {
                int j = rand.Next(i + 1);

                T tmp = arr[j];
                arr[j] = arr[i];
                arr[i] = tmp;
            }
        }

        private void UpdateCorrectAnswerList(Question question)
        {
            var index = question.IndexPrefab;

            Shuffle(question.Answers);
            var listIndexCorrectAnswer = question.GetCorrectAnswers();

            for (int i = 0; i < question.Answers.Length; i++)
            {

                AnswerData newAnswer = Instantiate(_answerPrefab[index], _uIElements.AnswerContentArea[index]);
                newAnswer.UpdateData(question.Answers[i].TranslateInfo, i);

                _currentAnswer.Add(newAnswer);
                Debug.Log(question._cadrCinemaName);

                if (question.GetAnswerType == AnswerType.Single)
                {
                    if (listIndexCorrectAnswer[0] == i)
                        _correctAnswer.Add(newAnswer);
                }
                else if (question.GetAnswerType == AnswerType.Multiply)
                {
                    for (int j = 0; j < listIndexCorrectAnswer.Count; j++)
                    {
                        if (i == listIndexCorrectAnswer[j])
                        {
                            _correctAnswer.Add(newAnswer);
                        }
                    }
                }
            }
        }

        private void EraseAnswers()
        {
            foreach(var answer in _currentAnswer)
            {
                Destroy(answer.gameObject);
            }
            _currentAnswer.Clear();
        }

        private void UpdateScoreUI()
        {
            if (_gameManager.CountCorrectAnswer <= _questionMethods.Data.Questions.Length)
                _uIElements.CountCurrentAnswer.text = _gameManager.CountCurrenttAnswer + "/" + _questionMethods.Data.Questions.Length;

            _uIElements.ScoreCurrentLvl.text = _score.CurrentLvlScore.ToString();

            _uIElements.ScoreFinalLvl.text = _score.CurrentLvlScore.ToString();
        }
    }
}
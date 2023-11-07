using SpaceShooter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TowerDefense;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuizCinema
{
    public class GameManager : MonoBehaviour, IDependency<QuestionMethods>, IDependency<Score>, IDependency<LvlData>, IDependency<TextMeshProUGUI>
    {
        public event Action<UIManager.ResolutionScreenType, int> UpdateDisplayScreenResolution;

        public event Action OnCorrectAnswer;
        public event Action OnUnCorrectAnswer;

        #region Variables
        [SerializeField] private QuestionMethods _questionMethods;
        [SerializeField] private Score _score;
        [SerializeField] private Animator _timerAnimator;
        [SerializeField] private Animator _loadingScreenAnimator;

        [SerializeField] private TextMeshProUGUI _timerText;

        private int levelCountStars = 3;
        private Color _timerDefaultColor = Color.white;

        [Header("Lvl")]
        [SerializeField] private LvlData _lvl;

        private int _loadingScreenStateParaHash = 0;
        private int _timerStateParaHash = 0;

        private IEnumerator IE_StartTimer = null;
        private IEnumerator IE_WaitTillNextRound = null;

        private WWW www;

        [SerializeField] private int _countCorrectAnswer = 0;
        public int CountCorrectAnswer { get { return _countCorrectAnswer; } set { _countCorrectAnswer = value; } }
        [SerializeField] private int _countCurrentAnswer = 1;
        public int CountCurrenttAnswer { get { return _countCurrentAnswer; } set { _countCurrentAnswer = value; } }
        #endregion

        public void Construct(QuestionMethods obj) 
        { 
            _questionMethods = obj;
            Debug.Log("Construct in GameManager");
        }

        public void Construct(Score obj)
        {
            _score = obj;
            Debug.Log("Construct in Score");
        }
        public void Construct(LvlData obj) => _lvl = obj;

        public void Construct(TextMeshProUGUI obj) 
        { 
            _timerText = obj;
            Debug.Log("Construct in TextMeshProUGUI");
        }

        private void Start()
        {
            _loadingScreenStateParaHash = Animator.StringToHash("Loading Screen");
           // _timerText.color = _timerDefaultColor;

            _timerStateParaHash = Animator.StringToHash("Timer State");
   
            StartCoroutine(Downloader(_lvl.Level)); //Call download data

            var seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            UnityEngine.Random.InitState(seed);
        }


        IEnumerator Downloader(int level)
        {
           
            _loadingScreenAnimator.SetInteger(_loadingScreenStateParaHash, 1);

            Debug.Log("StartDownloader");
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                www = new WWW("file://" + Application.streamingAssetsPath + $"/Q{SceneManager.GetActiveScene().buildIndex}.xml");
                Debug.Log("file://" + Application.streamingAssetsPath + $"/Q{SceneManager.GetActiveScene().buildIndex}.xml");
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                www = new WWW("jar:file://" + Application.dataPath + $"!/assets/Q{SceneManager.GetActiveScene().buildIndex}.xml");
            }

            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                www = new WWW("file://" + Application.dataPath + "/Raw" + $"/Q{SceneManager.GetActiveScene().buildIndex}.xml");
            }
            yield return new WaitForSeconds(2f); //will wait until the download finishes
            _loadingScreenAnimator.SetInteger(_loadingScreenStateParaHash, 0);
            _timerAnimator.enabled = true;

            Debug.Log("End download");

            if (www.isDone == true)
            {
                File.WriteAllBytes(Application.persistentDataPath + $"/Q{SceneManager.GetActiveScene().buildIndex}.xml", www.bytes);
                //_questionMethods.Data = Data.Fetch(Path.Combine(GameUtility.FileDir, GameUtility.FileName + $"{SceneManager.GetActiveScene().buildIndex}" + ".xml"));
                _questionMethods.Data = Data.Fetch(Path.Combine(Application.persistentDataPath, GameUtility.FileName + $"{SceneManager.GetActiveScene().buildIndex}" + ".xml"));
                _questionMethods.Display();
                Debug.Log("TryToDisplay");
            }
           
        }

        public void Accept()
        {
            UpdateTimer(false); 
            bool isCorrect =_questionMethods.CheckAnswers();

            if (isCorrect) 
                OnCorrectAnswer?.Invoke();
            else 
                OnUnCorrectAnswer?.Invoke();

            _questionMethods.FinishedQuestions.Add(_questionMethods.CurrentIndexQuestion);

            _score.UpdateScoreGame(isCorrect ? _questionMethods.Data.Questions[_questionMethods.CurrentIndexQuestion].AddScore : -_questionMethods.Data.Questions[_questionMethods.CurrentIndexQuestion].AddScore);

            FinishGame();

            if(IE_WaitTillNextRound != null)
            {
                StopCoroutine(IE_WaitTillNextRound);
            }

            AudioManager.Instance.PlaySound((isCorrect) ? "CorrectSFX" : "IncorrectSFX");

            var type = (_questionMethods.IsFinished) ? UIManager.ResolutionScreenType.Finish : (isCorrect) ? UIManager.ResolutionScreenType.Correct : UIManager.ResolutionScreenType.Incorrect;
            UpdateDisplayScreenResolution?.Invoke(type, _questionMethods.Data.Questions[_questionMethods.CurrentIndexQuestion].AddScore);

           // _timerAnimator.SetInteger(_timerStateParaHash, 1);
            //_timerAnimator.enabled = false;
        }

        public void NextQuestion()
        {
            //_timerAnimator.enabled = true;
            Debug.Log(_questionMethods.IsFinished);

            if (!_questionMethods.IsFinished)
            {
                IE_WaitTillNextRound = WaitTillNextRound();
                StartCoroutine(IE_WaitTillNextRound);
            }
            
            //_questionMethods.Display(); //TODO
        }

        private void FinishGame()
        {
            if (_questionMethods.IsFinished)
            {
                Debug.Log("FINISH GAME!");
                _lvl.Level++;

                Debug.Log(_lvl.Level);
                if (_lvl.Level > _lvl.MaxLevel)
                {
                    _lvl.Level = 1;
                }
                PlayerPrefs.SetInt(GameUtility.SavePrefLvlKey, _lvl.Level);

                if (_countCorrectAnswer == _questionMethods.FinishedQuestions.Count)
                {
                    levelCountStars = 3;
                }
                else if (_countCorrectAnswer >= _questionMethods.FinishedQuestions.Count / 2)
                {
                    levelCountStars = 2;
                }
                else if (_countCorrectAnswer >= _questionMethods.FinishedQuestions.Count / 4)
                {
                    levelCountStars = 1;
                }
                else
                {
                    levelCountStars = 0;
                }

                MapCompletion.SaveEpisodeResult(levelCountStars, _score.CurrentLvlScore);
            }
        }

        public void UpdateTimer(bool state)
        {
            switch (state)
            {
                case true:
                    IE_StartTimer = StartTimer();
                    StartCoroutine(IE_StartTimer);

                    _timerAnimator.SetInteger(_timerStateParaHash, 0);
                    break;

                case false:
                    if (IE_StartTimer != null)
                    {
                        StopCoroutine(IE_StartTimer);
                    }

                    _timerAnimator.SetInteger(_timerStateParaHash, 2);
                    break;
            }
        }

        IEnumerator StartTimer()
        {
            var totalTime = _questionMethods.Data.Questions[_questionMethods.CurrentIndexQuestion].Timer;
            var timeLeft = totalTime;
            _timerText.color = _timerDefaultColor;

            while (timeLeft > 0)
            {
                timeLeft--;

                AudioManager.Instance.PlaySound("CountdownSFX");

                if (timeLeft < totalTime / 2 && timeLeft > totalTime / 4)
                {
                   // _timerText.color = _timeHalfwayOutColor;
                }
                if (timeLeft < totalTime / 4)
                {
                    // _timerText.color = _timeAlmostOutColor;
                    //_timerAnimator.enabled = true;
                }

                _timerText.text = timeLeft.ToString();
                yield return new WaitForSeconds(1f);
            }
            Accept();
        }

        IEnumerator WaitTillNextRound()
        {
            yield return new WaitForSeconds(GameUtility.ResolutionDelayTime);
            Debug.Log("������ ����� ��������");
            bool isCorrect = _questionMethods.CheckAnswers();

            _countCurrentAnswer++;
            if (isCorrect)
            {
                _countCorrectAnswer++;
            }
            Debug.Log("�������� ��������");
            UpdateTimer(false);
            _questionMethods.Display();
            Debug.Log("Display");
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class FaultyAccelerandoScript : MonoBehaviour
{
    public KMAudio Audio;
    public AudioSource SequenceAudio;
    public KMBombInfo Bomb;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable Go;
    public KMSelectable Char;
    public GameObject BG;
    public GameObject Border;
    public TextMesh Char1;
    public TextMesh Char2;
    public TextMesh Lag;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    readonly Vector3[] pos = new Vector3[] { new Vector3(0f, 0.013f, 0f), new Vector3(0f, 0.013f, -0.0686f) };
    readonly Vector3[] scale = new Vector3[] { new Vector3(2f, 2f, 1.2f), new Vector3(1f, 1f, 0.6f) };

    bool active = false;
    bool correct;
    bool inputStarted;
    bool incorrect = false;
    bool newLetter = false;
    int timesCorrect = 0;
    int currentPair = 0;
    int lastPair = -1;
    int tries = 0;
    bool evenLagCheck = false;
    bool evenLags = false;
    Coroutine lag;
    List<float> stopsIn = new List<float>();
    List<float> stopsInLengths = new List<float>();
    List<int> stopsCycle = new List<int>();
    List<float> stopsCycleLengths = new List<float>();
    List<float> stopsOut = new List<float>();
    List<float> stopsOutLengths = new List<float>();
    readonly List<string> list = new List<string>();
    readonly List<string> listmod = new List<string>();
    List<int> listx = new List<int>();
    float speed = 1f;
    private Coroutine speedup;
    readonly List<int> positions = new List<int>();
    readonly float[] timings = new float[] { 1.439f, 1.361f, 1.295f, 1.243f, 1.165f, 1.112f, 1.073f, 1.02f, .981f, .942f, .903f, .877f, .837f, .811f, .799f, .758f, .733f, .72f, .693f, .694f };
    readonly List<string> letters = new List<string>();
    readonly List<int> numbers = new List<int>();
    readonly List<Pair> pairs = new List<Pair>();

    struct Pair
    {
        public string Letter;
        public int Number;
        public override string ToString()
        {
            return string.Format(@"{0}-{1}", Number, Letter);
        }
    }

    void Awake()
    {
        moduleId = moduleIdCounter++;
    }

    void Start()
    {
        var rnd = RuleSeedable.GetRNG();
        if (rnd.Seed == 1)
        {
            positions.AddRange(new[] { 2, 5, 8, 11 });
            evenLagCheck = true;
        }
        else
        {
            positions.AddRange(rnd.ShuffleFisherYates(Enumerable.Range(0, 12).ToList()).Take(4).OrderBy(v => v));
            int[] enc = new int[] { 0, 1 };
            enc = rnd.ShuffleFisherYates(enc);
            if (enc[0] == 0)
            {
                evenLagCheck = true;
            }
        }
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            evenLags = true;
        }

        Debug.LogFormat(@"[Faulty Accelerando #{0}] Using rule seed: {1}", moduleId, rnd.Seed);
        Debug.LogFormat(@"[Faulty Accelerando #{0}] Positions in distinct sums are: {1}", moduleId, positions.Select(x => x + 1).Join(", "));
        Debug.LogFormat(@"[Faulty Accelerando #{0}] Number of lags is: {1}", moduleId, evenLags ? "Even" : "Odd");

        letters.AddRange(Enumerable.Range(65, 26).Select(c => ((char) c).ToString()));
        numbers.AddRange(Enumerable.Range(1, 20));

        Char1.transform.parent.gameObject.SetActive(false);
        Char2.transform.parent.gameObject.SetActive(false);

        Generate();

        Go.OnInteract += delegate
        {
            if (moduleSolved || active)
                return false;
            Go.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Go.transform);
            StartCoroutine(Sequence());
            StartCoroutine(MoveScale(true));
            pairs.Shuffle();
            listx.Clear();
            list.Clear();
            listmod.Clear();
            listx = pairs.Select(c => ((c.Letter[0] - 'A' + 1) + c.Number) > 26 ? ((c.Letter[0] - 'A' + 1) + c.Number) - 26 : ((c.Letter[0] - 'A' + 1) + c.Number)).ToList();
            listx = listx.Distinct().ToList();
            listx.Sort();
            listx.Reverse();
            for (int i = 0; i < 4; i++)
            {
                list.Add(NumberToString(listx[positions[i]]));
            }
            Debug.LogFormat(@"[Faulty Accelerando #{0}] Pairs are: {1}", moduleId, string.Join(", ", pairs.Select(pair => pair.ToString()).ToArray()));
            Debug.LogFormat(@"[Faulty Accelerando #{0}] Distinct sums are: {1}", moduleId, listx.Join(", ").ToString());
            Debug.LogFormat(@"[Faulty Accelerando #{0}] Letters received from positions in distinct sums list: {1}", moduleId, list.Join(", ").ToString());
            if (evenLagCheck)
            {
                if (evenLags)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        listmod.Add(oppositeLetter(NumberToString(listx[positions[i]])));
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        listmod.Add(rot13Letter(NumberToString(listx[positions[i]])));
                    }
                }
            }
            else
            {
                if (evenLags)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        listmod.Add(rot13Letter(NumberToString(listx[positions[i]])));
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        listmod.Add(oppositeLetter(NumberToString(listx[positions[i]])));
                    }
                }
            }
            Debug.LogFormat(@"[Faulty Accelerando #{0}] After lag modifications, press the numbers that have the following letters: {1}", moduleId, listmod.Join(", ").ToString());
            active = true;
            return false;
        };

        Char.OnInteract += delegate
        {
            Char.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Char.transform);
            inputStarted = true;
            if (correct)
            {
                if (currentPair == lastPair)
                    Debug.LogFormat(@"[Faulty Accelerando #{0}] Tried to press {2}-{1} again.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                else
                {
                    lastPair = currentPair;
                    Debug.LogFormat(@"[Faulty Accelerando #{0}] Correctly pressed {2}-{1}.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                    timesCorrect++;
                };
            }
            else
            {
                incorrect = true;
                if (currentPair == lastPair)
                    Debug.LogFormat(@"[Faulty Accelerando #{0}] Tried to press {2}-{1} again.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                else
                {
                    lastPair = currentPair;
                    Debug.LogFormat(@"[Faulty Accelerando #{0}] Incorrectly pressed {2}-{1}.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                };
            }

            return false;
        };
    }

    void Generate()
    {
        tryAgain:
        pairs.Clear();
        listx.Clear();
        letters.Shuffle();
        numbers.Shuffle();

        for (int i = 0; i < 20; i++)
        {
            pairs.Add(new Pair { Letter = letters[i], Number = numbers[i] });
        }

        listx = pairs.Select(c => ((c.Letter[0] - 'A' + 1) + c.Number) > 26 ? ((c.Letter[0] - 'A' + 1) + c.Number) - 26 : ((c.Letter[0] - 'A' + 1) + c.Number)).ToList();
        listx = listx.Distinct().ToList();

        if (listx.Count < 12)
        {
            Debug.LogFormat(@"<Faulty Accelerando #{0}> Distinct list is too small. Generating new!", moduleId);
            tries++;
            if (tries < 500)
                goto tryAgain;
        }

        List<int> templistx = listx;
        templistx.Sort();
        templistx.Reverse();
        for (int j = 0; j < 4; j++)
        {
            bool found = false;
            for (int i = 0; i < 20; i++)
            {
                if (evenLagCheck)
                {
                    if (evenLags)
                    {
                        if (pairs[i].Letter.Equals(oppositeLetter(NumberToString(templistx[positions[j]]))))
                        {
                            found = true;
                        }
                    }
                    else
                    {
                        if (pairs[i].Letter.Equals(rot13Letter(NumberToString(templistx[positions[j]]))))
                        {
                            found = true;
                        }
                    }
                }
                else
                {
                    if (evenLags)
                    {
                        if (pairs[i].Letter.Equals(rot13Letter(NumberToString(templistx[positions[j]]))))
                        {
                            found = true;
                        }
                    }
                    else
                    {
                        if (pairs[i].Letter.Equals(oppositeLetter(NumberToString(templistx[positions[j]]))))
                        {
                            found = true;
                        }
                    }
                }
            }
            if (!found)
            {
                Debug.LogFormat(@"<Faulty Accelerando #{0}> At least one generated letter is not an option. Generating new!", moduleId);
                tries++;
                if (tries < 500)
                    goto tryAgain;
            }
        }

        if (tries >= 500)
        {
            Debug.LogFormat(@"<Faulty Accelerando #{0}> Couldn't find a solution after 500 tries. Using default set!", moduleId);
            pairs.Clear();
            listx.Clear();
            pairs.Add(new Pair { Letter = "J", Number = 15 });
            pairs.Add(new Pair { Letter = "I", Number = 13 });
            pairs.Add(new Pair { Letter = "Q", Number = 8 });
            pairs.Add(new Pair { Letter = "D", Number = 2 });
            pairs.Add(new Pair { Letter = "T", Number = 17 });
            pairs.Add(new Pair { Letter = "U", Number = 1 });
            pairs.Add(new Pair { Letter = "B", Number = 12 });
            pairs.Add(new Pair { Letter = "W", Number = 19 });
            pairs.Add(new Pair { Letter = "V", Number = 14 });
            pairs.Add(new Pair { Letter = "L", Number = 16 });
            pairs.Add(new Pair { Letter = "E", Number = 6 });
            pairs.Add(new Pair { Letter = "A", Number = 11 });
            pairs.Add(new Pair { Letter = "N", Number = 3 });
            pairs.Add(new Pair { Letter = "R", Number = 4 });
            pairs.Add(new Pair { Letter = "X", Number = 9 });
            pairs.Add(new Pair { Letter = "S", Number = 7 });
            pairs.Add(new Pair { Letter = "F", Number = 20 });
            pairs.Add(new Pair { Letter = "Z", Number = 10 });
            pairs.Add(new Pair { Letter = "Y", Number = 18 });
            pairs.Add(new Pair { Letter = "M", Number = 5 });
            listx = pairs.Select(c => ((c.Letter[0] - 'A' + 1) + c.Number) > 26 ? ((c.Letter[0] - 'A' + 1) + c.Number) - 26 : ((c.Letter[0] - 'A' + 1) + c.Number)).ToList();
            listx = listx.Distinct().ToList();
        }
        if (tries < 500)
            Debug.LogFormat(@"<Faulty Accelerando #{0}> Number of tries: {1}", moduleId, tries.ToString());
    }

    private string NumberToString(int number)
    {
        char c = (char)((65) + (number - 1));
        return c.ToString();
    }

    private string oppositeLetter(string s)
    {
        string[] letters = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        int index = Array.IndexOf(letters, s);
        int ct = 25;
        for (int i = 0; i < index; i++)
        {
            ct--;
        }
        return letters[ct];
    }

    private string rot13Letter(string s)
    {
        string[] letters = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
        int index = Array.IndexOf(letters, s);
        for (int i = 0; i < 13; i++)
        {
            index++;
            if (index > 25)
            {
                index = 0;
            }
        }
        return letters[index];
    }

    private bool inRange(List<float> list, float check)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (check > (list[i]-.3) && check < (list[i] + .3))
            {
                return true;
            }
        }
        return false;
    }

    private bool canAddAnother(List<int> list)
    {
        bool[] status = new bool[20];
        for (int i = 0; i < list.Count; i++)
        {
            status[list[0]] = true;
            if (list[0] - 1 >= 0)
                status[list[0] - 1] = true;
            if (list[0] + 1 <= 18)
                status[list[0] + 1] = true;
            if (list[0] - 2 >= 0)
                status[list[0] - 2] = true;
            if (list[0] + 2 <= 18)
                status[list[0] + 2] = true;
        }
        if (status.Contains(false))
            return true;
        return false;
    }

    IEnumerator Sequence()
    {
        SequenceAudio.Play();
        stopsIn.Clear();
        stopsInLengths.Clear();
        stopsOut.Clear();
        stopsOutLengths.Clear();
        int totallags = 0;
        int temp = UnityEngine.Random.Range(2, 4);
        totallags += temp;
        for (int i = 0; i < temp; i++)
        {
            float rand = UnityEngine.Random.Range(0.3f, 3.15f);
            while (inRange(stopsIn, rand))
                rand = UnityEngine.Random.Range(0.3f, 3.15f);
            stopsIn.Add(rand);
            stopsInLengths.Add(UnityEngine.Random.Range(0.3f, 0.7f));
        }
        stopsIn.Sort();
        temp = UnityEngine.Random.Range(3, 6);
        totallags += temp;
        retry:
        stopsCycle.Clear();
        stopsCycleLengths.Clear();
        for (int i = 0; i < temp; i++)
        {
            if (temp == 4)
            {
                if (!canAddAnother(stopsCycle))
                    goto retry;
            }
            int rand = UnityEngine.Random.Range(0, 19);
            while (stopsCycle.Contains(rand) || stopsCycle.Contains(rand-1) || stopsCycle.Contains(rand+1) || stopsCycle.Contains(rand-2) || stopsCycle.Contains(rand+2))
                rand = UnityEngine.Random.Range(0, 19);
            stopsCycle.Add(rand);
            stopsCycleLengths.Add(UnityEngine.Random.Range(1f, 1.6f));
        }
        stopsCycle.Sort();
        temp = UnityEngine.Random.Range(2, 4);
        if (evenLags)
            while ((temp + totallags) % 2 != 0) { temp = UnityEngine.Random.Range(2, 4); }
        else
            while ((temp + totallags) % 2 == 0) { temp = UnityEngine.Random.Range(2, 4); }
        totallags += temp;
        for (int i = 0; i < temp; i++)
        {
            float rand = UnityEngine.Random.Range(0.3f, 2.197f);
            while (inRange(stopsOut, rand))
                rand = UnityEngine.Random.Range(0.3f, 2.197f);
            stopsOut.Add(rand);
            stopsOutLengths.Add(UnityEngine.Random.Range(0.3f, 0.7f));
        }
        stopsOut.Sort();
        yield return new WaitUntil(() => SequenceAudio.time > 22.896f);
        yield return StartCoroutine(MoveScale(false));
        if (inputStarted)
        {
            if (timesCorrect == list.Count && incorrect == false)
            {
                Debug.LogFormat(@"[Faulty Accelerando #{0}] You pressed every required number correctly. Well Done.", moduleId);
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
            }
            else
            {
                Debug.LogFormat(@"[Faulty Accelerando #{0}] You didn't press every required number correctly. Strike.", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
                inputStarted = false;
                incorrect = false;
                timesCorrect = 0;
                currentPair = 0;
                lastPair = -1;
                Generate();
            }
        }

        active = false;
        yield break;
    }

    IEnumerator MoveScale(bool mode)
    {
        float duration;
        if (mode)
            duration = 3.45f;
        else
        {
            duration = 2.497f;
            SequenceAudio.pitch = 1f;
        }
        var passed = 0;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            float elapsed2;
            if (mode)
            {
                if (passed != stopsIn.Count())
                {
                    if (elapsed >= stopsIn[passed])
                    {
                        SequenceAudio.Pause();
                        elapsed2 = 0f;
                        while (elapsed2 < stopsInLengths[passed])
                        {
                            yield return null;
                            elapsed2 += Time.deltaTime;
                        }
                        SequenceAudio.UnPause();
                        passed++;
                    }
                }
                Go.transform.localScale = Vector3.Lerp(scale[0], scale[1], elapsed / duration);
                Go.transform.localPosition = Vector3.Lerp(pos[0], pos[1], elapsed / duration);
                Go.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.black, Color.white, elapsed / duration);
                Go.GetComponentInChildren<TextMesh>().color = Color32.Lerp(Color.white, Color.black, elapsed / duration);
                BG.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.white, Color.black, elapsed / duration);
                Border.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.white, Color.black, elapsed / duration);
            }
            else
            {
                if (passed != stopsOut.Count())
                {
                    if (elapsed >= stopsOut[passed])
                    {
                        SequenceAudio.Pause();
                        elapsed2 = 0f;
                        while (elapsed2 < stopsOutLengths[passed])
                        {
                            yield return null;
                            elapsed2 += Time.deltaTime;
                        }
                        SequenceAudio.UnPause();
                        passed++;
                    }
                }
                Go.transform.localScale = Vector3.Lerp(scale[1], scale[0], elapsed / duration);
                Go.transform.localPosition = Vector3.Lerp(pos[1], pos[0], elapsed / duration);
                Go.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.white, Color.black, elapsed / duration);
                Go.GetComponentInChildren<TextMesh>().color = Color32.Lerp(Color.black, Color.white, elapsed / duration);
                BG.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.black, Color.white, elapsed / duration);
                Border.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.black, Color.white, elapsed / duration);
            }
        }
        if (mode)
            StartCoroutine(Cycle());
    }

    IEnumerator Cycle()
    {
        Char1.transform.parent.gameObject.SetActive(true);
        Char2.transform.parent.gameObject.SetActive(true);
        var passed = 0;
        for (int i = 0; i < 20; i++)
        {
            currentPair = i;
            newLetter = true;
            if (listmod.Contains(pairs[i].Letter))
                correct = true;
            else
                correct = false;
            Char1.text = pairs[i].Number.ToString();
            Char2.text = pairs[i].Letter;
            GetComponent<KMSelectable>().UpdateChildren();
            SequenceAudio.pitch = 1f * speed;
            var elapsed = 0f;
            var time = timings[i] / speed;
            while (elapsed < time)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            if (passed != stopsCycle.Count())
            {
                if (stopsCycle[passed] == i)
                {
                    if (speedup != null)
                        StopCoroutine(speedup);
                    SequenceAudio.Pause();
                    Char1.text = "";
                    Char2.text = "";
                    lag = StartCoroutine(Lagger());
                    elapsed = 0f;
                    while (elapsed < stopsCycleLengths[passed])
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                    }
                    SequenceAudio.UnPause();
                    StopCoroutine(lag);
                    Lag.text = "";
                    passed++;
                    speed = 2f;
                    speedup = StartCoroutine(speedUp());
                }
            }
        }
        newLetter = false;
        Char1.text = "";
        Char2.text = "";
        Char1.transform.parent.gameObject.SetActive(false);
        Char2.transform.parent.gameObject.SetActive(false);
    }

    private IEnumerator speedUp()
    {
        while (speed > 1f)
        {
            speed -= .1f;
            var elapsed = 0f;
            while (elapsed < .1f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }

    IEnumerator Lagger()
    {
        while (true)
        {
            Lag.text = ".";
            var elapsed = 0f;
            while (elapsed < .2f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            Lag.text = "..";
            elapsed = 0f;
            while (elapsed < .2f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            Lag.text = "...";
            elapsed = 0f;
            while (elapsed < .2f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            Lag.text = "";
            elapsed = 0f;
            while (elapsed < .2f)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} go/start [press the GO button] | !{0} 3 15 10 [press these numbers (note: This will also press the GO button)]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if (moduleSolved)
        {
            yield return null;
            yield return "sendtochaterror The module is already solved.";
            yield break;
        }
        else if (active)
        {
            yield return null;
            yield return "sendtochaterror The module is currently displaying its sequence.";
            yield break;
        }
        else if (Regex.IsMatch(command, @"^\s*(go|start)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Go.OnInteract();
            yield break;
        }
        else
        {
            yield return null;
            string[] nums = command.Split(' ');
            for (int i = 0; i < nums.Length; i++)
            {
                int temp = 0;
                if (!int.TryParse(nums[i], out temp))
                {
                    yield return "sendtochaterror Invalid Command.";
                    yield break;
                }
                if (temp < 1 || temp > 20)
                {
                    yield return "sendtochaterror Invalid Command.";
                    yield break;
                }
            }
            Go.OnInteract();
            for (int i = 0; i < nums.Length;)
            {
                yield return true;
                yield return new WaitUntil(() => newLetter || !active);
                if (!active)
                    yield break;
                newLetter = false;
                if (nums.Contains(pairs[currentPair].Number.ToString()))
                {
                    Char.OnInteract();
                    i++;
                }
            }
            if (timesCorrect == 4)
            {
                yield return "solve";
            }
            else
            {
                yield return "strike";
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat(@"[Faulty Accelerando #{0}] Module was force solved by TP.", moduleId);
        while (active) { yield return true; }
        Go.OnInteract();
        for (int i = 0; i < list.Count();)
        {
            yield return new WaitUntil(() => newLetter);
            newLetter = false;
            if (listmod.Contains(pairs[currentPair].Letter))
            {
                Char.OnInteract();
                i++;
            }
        }
        while (!moduleSolved) { yield return true; }
        yield break;
    }
}
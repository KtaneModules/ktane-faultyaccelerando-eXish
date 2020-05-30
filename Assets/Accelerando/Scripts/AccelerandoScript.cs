using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

public class AccelerandoScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMRuleSeedable RuleSeedable;

    public KMSelectable Go;
    public KMSelectable Char;
    public GameObject BG;
    public GameObject Border;
    public TextMesh Char1;
    public TextMesh Char2;

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
    List<int> list = new List<int>();
    List<int> listx = new List<int>();
    List<int> positions = new List<int>();
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
            positions.Add(2);
            positions.Add(5);
            positions.Add(8);
            positions.Add(11);
        }
        else
        {
            var x = Enumerable.Range(0, 12).ToList();
            for (int i = 0; i < 4; i++)
            {
                var ix = rnd.Next(0, x.Count);
                positions.Add(x[ix]);
                x.RemoveAt(ix);
            }
            positions.Sort();
        }
        Debug.LogFormat(@"[Accelerando #{0}] Ruleseed: {1}", moduleId, rnd.Seed);
        Debug.LogFormat(@"[Accelerando #{0}] Positions in distinct differences are: {1}", moduleId, positions.Select(x => x + 1).Join(", "));

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
            listx = pairs.Select(c => Math.Abs((c.Letter[0] - 'A' + 1) - c.Number)).ToList();
            listx = listx.Distinct().ToList();
            listx.Sort();
            for (int i = 0; i < 4; i++)
            {
                list.Add(listx[positions[i]]);
            }
            Debug.LogFormat(@"[Accelerando #{0}] Pairs are: {1}", moduleId, string.Join(", ", pairs.Select(pair => pair.ToString()).ToArray()));
            Debug.LogFormat(@"[Accelerando #{0}] Distinct differences are: {1}", moduleId, listx.Join(", ").ToString());
            Debug.LogFormat(@"[Accelerando #{0}] Press the letters that have the following numbers: {1}", moduleId, list.Join(", ").ToString());
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
                    Debug.LogFormat(@"[Accelerando #{0}] Tried to press {2}-{1} again.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                else
                {
                    lastPair = currentPair;
                    Debug.LogFormat(@"[Accelerando #{0}] Correctly pressed {2}-{1}.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                    timesCorrect++;
                };
            }
            else
            {
                incorrect = true;
                if (currentPair == lastPair)
                    Debug.LogFormat(@"[Accelerando #{0}] Tried to press {2}-{1} again.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                else
                {
                    lastPair = currentPair;
                    Debug.LogFormat(@"[Accelerando #{0}] Incorrectly pressed {2}-{1}.", moduleId, pairs[currentPair].Letter, pairs[currentPair].Number.ToString());
                };
            }

            return false;
        };
    }

    void Generate()
    {
        tryAgain:
        pairs.Clear();
        list.Clear();
        listx.Clear();
        letters.Shuffle();
        numbers.Shuffle();

        for (int i = 0; i < 20; i++)
        {
            if (letters[i][0] - 'A' + 1 == numbers[i])
            {
                Debug.LogFormat(@"<Accelerando #{0}> At least one pair has a difference of 0. Generating new!", moduleId);
                tries++;
                if (tries < 500)
                    goto tryAgain;
            }
        }

        for (int i = 0; i < 20; i++)
        {
            pairs.Add(new Pair { Letter = letters[i], Number = numbers[i] });
        }

        listx = pairs.Select(c => Math.Abs((c.Letter[0] - 'A' + 1) - c.Number)).ToList();
        listx = listx.Distinct().ToList();

        if (listx.Count < 12)
        {
            Debug.LogFormat(@"<Accelerando #{0}> Distinct list is too small. Generating new!", moduleId);
            tries++;
            if (tries < 500)
                goto tryAgain;
        }

        for (int i = 0; i < listx.Count; i++)
        {
            if (listx[i] > 20)
            {
                Debug.LogFormat(@"<Accelerando #{0}> At least one entry in the distinct list is greater than 20. Generating new!", moduleId);
                tries++;
                if (tries < 5000)
                    goto tryAgain;
            }
        }

        if (tries >= 500)
        {
            Debug.LogFormat(@"<Accelerando #{0}> Couldn't find a solution after 50000 tries. Using default set!", moduleId);
            pairs.Clear();
            listx.Clear();
            list.Clear();
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
            listx = pairs.Select(c => Math.Abs((c.Letter[0] - 'A' + 1) - c.Number)).ToList();
            listx = listx.Distinct().ToList();
        }
        if (tries < 500)
            Debug.LogFormat(@"<Accelerando #{0}> Number of tries: {1}", moduleId, tries.ToString());
    }

    IEnumerator Sequence()
    {
        Audio.PlaySoundAtTransform("Sequence", GetComponent<KMBombModule>().transform);
        yield return new WaitForSeconds(22.896f);
        yield return StartCoroutine(MoveScale(false));
        if (inputStarted)
        {
            if (timesCorrect == list.Count && incorrect == false)
            {
                Debug.LogFormat(@"[Accelerando #{0}] You pressed every required letter correctly. Well Done.", moduleId);
                GetComponent<KMBombModule>().HandlePass();
                moduleSolved = true;
            }
            else
            {
                Debug.LogFormat(@"[Accelerando #{0}] You didn't press every required letter correctly. Strike.", moduleId);
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
        {
            duration = 3.45f;
        }
        else
            duration = 2.497f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (mode)
            {
                Go.transform.localScale = Vector3.Lerp(scale[0], scale[1], elapsed / duration);
                Go.transform.localPosition = Vector3.Lerp(pos[0], pos[1], elapsed / duration);
                Go.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.black, Color.white, elapsed / duration);
                Go.GetComponentInChildren<TextMesh>().color = Color32.Lerp(Color.white, Color.black, elapsed / duration);
                BG.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.white, Color.black, elapsed / duration);
                Border.GetComponent<MeshRenderer>().material.color = Color32.Lerp(Color.white, Color.black, elapsed / duration);
            }
            else
            {
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
        for (int i = 0; i < 20; i++)
        {
            currentPair = i;
            newLetter = true;
            if (list.Contains(pairs[i].Number))
                correct = true;
            else
                correct = false;
            Char1.text = pairs[i].Number.ToString();
            Char2.text = pairs[i].Letter;
            GetComponent<KMSelectable>().UpdateChildren();
            yield return new WaitForSeconds(timings[i]);
        }
        newLetter = false;
        Char1.text = "";
        Char2.text = "";
        Char1.transform.parent.gameObject.SetActive(false);
        Char2.transform.parent.gameObject.SetActive(false);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} go/start [press the GO button] | !{0} WLMN [press these letters (note: This will also press the GO button)]";
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
        else if ((m = Regex.Match(command, @"^\s*[ABCDEFGHIJKLMNOPQRSTUVWXYZ]+\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            var letters = m.Groups[0].ToString();
            Go.OnInteract();
            for (int i = 0; i < letters.Length;)
            {
                yield return true;
                yield return new WaitUntil(() => newLetter || !active);
                if (!active)
                    yield break;
                newLetter = false;
                if (letters.Contains(pairs[currentPair].Letter))
                {
                    Char.OnInteract();
                    i++;
                }
            }
            yield break;
        }
        else
        {
            yield return "sendtochaterror Invalid Command.";
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.LogFormat(@"[Yes and No #{0}] Module was force solved by TP.", moduleId);
        Go.OnInteract();
        for (int i = 0; i < list.Count();)
        {
            yield return true;
            yield return new WaitUntil(() => newLetter);
            newLetter = false;
            if (list.Contains(pairs[currentPair].Number))
            {
                Char.OnInteract();
                i++;
            }
        }
        yield break;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class CyanArrowsScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
	public KMBombModule Module;
    public KMColorblindMode Colorblind;

    public KMSelectable[] buttons;
	public KMSelectable Display;
	public AudioClip[] SFX;
	
    public GameObject numDisplay;
    public GameObject colorblindText;
	public Material[] ColorArrows;
	public MeshRenderer[] ArrowHeads;
	public Sprite Play;
	public SpriteRenderer Play2;
	public AudioSource MusicPlayer;

    bool Interactable = true, SubmittablePhase = false;
	private bool ColorBlindActive = false;
	
	List<int> NumberOfTurns = new List<int>();
	List<string> Movement = new List<string>();
	List<string> Orientation = new List<string>();
	List<string> CorrectDirections = new List<string>();
	List<string> Walls = new List<string>();
	List<string> SubmittedWalls = new List<string>();
	string[] Directions = {"U", "R", "D", "L"};
	char[] DirectionsChar = {'U', 'R', 'D', 'L'};
	Coroutine Jukebox, ColorParty;

	string TempWall = "";
	int TheNumber = 20, MarchNumber = 0;
	
    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved = false;
	
	Coroutine Waiter;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        for (int a = 0; a < buttons.Count(); a++)
        {
            int ArrowPos = a;
            buttons[ArrowPos].OnInteract += delegate
            {
                PressArrow(ArrowPos);
				return false;
            };
        }
		Display.OnInteract += delegate
		{
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			Display.AddInteractionPunch(0.25f);
			if (Interactable && !ModuleSolved)
			{
				if (!SubmittablePhase)
				{
					Waiter = StartCoroutine(WaitForTime());
				}
				
				else
				{
					TempWall = SortString(TempWall);
					SubmittedWalls.Add(TempWall);
					TempWall = "";
					
					if (numDisplay.GetComponent<TextMesh>().text == "20")
					{
						Interactable = false;
						string AddToLog2 = "Walls Assembled: ";
						for (int x = 0; x < SubmittedWalls.Count(); x++)
						{
							AddToLog2 += "Wall " + (x + 1).ToString() + ": ";
							AddToLog2 += SubmittedWalls[x].Length == 0 ? "[EMPTY]" : SubmittedWalls[x];
							AddToLog2 += x < SubmittedWalls.Count() - 1 ? " / " : "";
						}
						Debug.LogFormat("[Cyan Arrows #{0}] {1}", moduleId, AddToLog2);
						StartCoroutine(March());
					}
					
					else
					{
						numDisplay.GetComponent<TextMesh>().text = int.Parse(numDisplay.GetComponent<TextMesh>().text) + 1 < 10 ? "0" + (int.Parse(numDisplay.GetComponent<TextMesh>().text) + 1).ToString() : (int.Parse(numDisplay.GetComponent<TextMesh>().text) + 1).ToString();
						foreach (MeshRenderer Render in ArrowHeads)
						{
							Render.material = ColorArrows[0];
						}
					}
				}
			}
			return false;
		};
		
		Display.OnInteractEnded += delegate
		{
			if (Interactable && !ModuleSolved && !SubmittablePhase)
			{
				if (Waiter != null)
				{
					StopCoroutine(Waiter);
				}
				
                if (TheNumber != 0)
				{
					StartCoroutine(PressDisplay());
				}
				
				else
				{
					SubmittablePhase = true;
					Interactable = false;
					StartCoroutine(Transition());
				}
			}
		};
	}
	
	void Start()
    {
        Generate();
		StartCoroutine(ColorblindDelay());
    }
	
    void Generate()
	{
		int Numerical = 20, AmountOfLoop = 1;
		while (Numerical != 0)
		{
			if (Numerical > 5)
			{
				int Random = UnityEngine.Random.Range(3,6);
				NumberOfTurns.Add(Random);
				Numerical -= Random;
			}
			
			else
			{
				if (Numerical < 3)
				{
					NumberOfTurns = new List<int>();
					Numerical = 20;
					AmountOfLoop++;
				}
				
				else
				{
					NumberOfTurns.Add(Numerical);
					Numerical -= Numerical;
				}
			}
		}
		
		Debug.LogFormat("[Cyan Arrows #{0}] ----------------------------------------------------------------------", moduleId);
		int OrientationCount = 0, MovementCount = 0;
		for (int x = 0; x < NumberOfTurns.Count(); x++)
		{
			string MovementArrows = "", PreviousMovement = "";
			Orientation.Add(Directions[UnityEngine.Random.Range(0,4)]);
			OrientationCount++;
			if (Orientation.Count() > 1)
			{
				while (Orientation[Orientation.Count() - 1] == Orientation[Orientation.Count() - 2])
				{
					Orientation[Orientation.Count() - 1] = Directions[UnityEngine.Random.Range(0,4)];
				}
			}
			
			while (MovementArrows.Length < NumberOfTurns[x] - 1)
			{
				string Type = Directions[UnityEngine.Random.Range(0,4)];
				while (Type == PreviousMovement)
				{
					Type = Directions[UnityEngine.Random.Range(0,4)];
				}
				MovementArrows += Type;
				PreviousMovement = Directions[((Array.IndexOf(Directions, Type)) + 2) % 4];
			}
			MovementCount++;
			Movement.Add(MovementArrows);
			Debug.LogFormat("[Cyan Arrows #{0}] Orientation {2}: {1} / Movement {4}: {3}", moduleId, Orientation[x], OrientationCount.ToString(), MovementArrows, MovementCount.ToString());
		}
		
		int WallCount = 0;
		for (int x = 0; x < Orientation.Count(); x++)
		{
			string AddToLog = "";
			string PreviousMovement2 = "";
			for (int y = 0; y < Movement[x].Length + 1; y++)
			{
				WallCount++;
				string FullWall = "URDL";
				
				if (y > 0)
				{
					FullWall = FullWall.Replace(PreviousMovement2, String.Empty);
				}
				
				if (y < Movement[x].Length)
				{
					CorrectDirections.Add(Directions[(Array.IndexOf(Directions, Movement[x][y].ToString()) + Array.IndexOf(Directions, Orientation[x])) % 4]);
					FullWall = FullWall.Replace(Directions[(Array.IndexOf(Directions, Movement[x][y].ToString()) + Array.IndexOf(Directions, Orientation[x])) % 4], String.Empty);
					PreviousMovement2 = Directions[((Array.IndexOf(Directions, Movement[x][y].ToString()) + Array.IndexOf(Directions, Orientation[x])) + 2) % 4];
				}
				
				else
				{
					CorrectDirections.Add("[EMPTY]");
				}
				
				FullWall = SortString(FullWall);
				Walls.Add(FullWall);
				AddToLog += "Wall " + WallCount.ToString() + ": " + FullWall;
				AddToLog += y < Movement[x].Length ? " / " : "";
				
			}
			Debug.LogFormat("[Cyan Arrows #{0}] {1}", moduleId, AddToLog);
		}
	}
	
	IEnumerator WaitForTime()
    {
        if (Interactable && !ModuleSolved)
        {
            while (TheNumber != 0)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                TheNumber--;
            }
			Play2.sprite = null;
			
			while (true)
			{
				foreach (MeshRenderer Render in ArrowHeads)
				{
					Render.material = ColorArrows[4];
				}
				Audio.PlaySoundAtTransform(SFX[2].name, transform);
				yield return new WaitForSecondsRealtime(0.1f);
				
				foreach (MeshRenderer Render2 in ArrowHeads)
				{
					Render2.material = ColorArrows[0];
				}
				yield return new WaitForSecondsRealtime(0.1f);
			}
        }
    }

	void PressArrow(int ArrowPos)
	{
		buttons[ArrowPos].AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (Interactable && !ModuleSolved && SubmittablePhase)
		{
			TempWall = TempWall.ToCharArray().Count(c => c == DirectionsChar[ArrowPos]) != 0 ? TempWall.Replace(Directions[ArrowPos], "") : TempWall + Directions[ArrowPos];
			ArrowHeads[ArrowPos].material = TempWall.ToCharArray().Count(c => c == DirectionsChar[ArrowPos]) != 0 ? ColorArrows[3] : ColorArrows[0];
		}
	}
	
	IEnumerator PressDisplay()
	{
		TheNumber = 20;
		Display.AddInteractionPunch(0.25f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		if (Interactable && !ModuleSolved)
		{
			Interactable = false;
			Play2.sprite = null;
			yield return new WaitForSecondsRealtime(0.5f);
			int Start = 0;
			for (int x = 0; x < NumberOfTurns.Count(); x++)
			{
				
				for (int y = 0; y < 5; y++)
				{
					numDisplay.GetComponent<TextMesh>().text = x == 0 ? "-" : Start < 10 ? "0" + Start.ToString() : Start.ToString();
					ArrowHeads[Array.IndexOf(Directions, Orientation[x])].material = ColorArrows[1];
					Audio.PlaySoundAtTransform(SFX[1].name, transform);
					yield return new WaitForSecondsRealtime(0.1f);
					ArrowHeads[Array.IndexOf(Directions, Orientation[x])].material = ColorArrows[0];
					yield return new WaitForSecondsRealtime(0.1f);
				}
				Start++;
				
				for (int z = 0; z < NumberOfTurns[x] - 1; z++)
				{
					numDisplay.GetComponent<TextMesh>().text = Start < 10 ? "0" + Start.ToString() : Start.ToString();
					ArrowHeads[Array.IndexOf(Directions, Movement[x][z].ToString())].material = ColorArrows[2];
					Audio.PlaySoundAtTransform(SFX[0].name, transform);
					yield return new WaitForSecondsRealtime(0.25f);
					ArrowHeads[Array.IndexOf(Directions, Movement[x][z].ToString())].material = ColorArrows[0];
					yield return new WaitForSecondsRealtime(0.25f);
					Start++;
				}
			}
			
			for (int y = 0; y < 5; y++)
			{
				numDisplay.GetComponent<TextMesh>().text = "20";
				foreach (MeshRenderer Render in ArrowHeads)
				{
					Render.material = ColorArrows[3];
				}
				Audio.PlaySoundAtTransform(SFX[1].name, transform);
				yield return new WaitForSecondsRealtime(0.1f);
				
				foreach (MeshRenderer Render2 in ArrowHeads)
				{
					Render2.material = ColorArrows[0];
				}
				yield return new WaitForSecondsRealtime(0.1f);
			}
				
			numDisplay.GetComponent<TextMesh>().text = "";
			yield return new WaitForSecondsRealtime(0.5f);
			Interactable = true;
			Play2.sprite = Play;
		}
	}

	private IEnumerator ColorblindDelay()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        ColorBlindActive = Colorblind.ColorblindModeActive;
        if (ColorBlindActive)
        {
            Debug.LogFormat("[Cyan Arrows #{0}] Colorblind mode active!", moduleId);
            colorblindText.SetActive(true);
        }
    }
	
	IEnumerator Transition()
	{
		MusicPlayer.clip = SFX[3];
		MusicPlayer.Play();
		foreach (MeshRenderer Render in ArrowHeads)
		{
			Render.material = ColorArrows[3];
		}
		while(MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(0.01f);
		}
		
		MusicPlayer.clip = SFX[4];
		MusicPlayer.Play();
		foreach (MeshRenderer Render in ArrowHeads)
		{
			Render.material = ColorArrows[0];
		}
		numDisplay.GetComponent<TextMesh>().text = "01";
		while(MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(0.01f);
		}
		Interactable = true;
	}
	
	IEnumerator March()
	{
		MusicPlayer.clip = SFX[3];
		MusicPlayer.Play();
		numDisplay.GetComponent<TextMesh>().text = "";
		foreach (MeshRenderer Render in ArrowHeads)
		{
			Render.material = ColorArrows[3];
		}
		while(MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(0.01f);
		}
		MusicPlayer.clip = SFX[4];
		MusicPlayer.Play();
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material = SubmittedWalls[MarchNumber].Contains(DirectionsChar[x]) ? ColorArrows[3] : ColorArrows[0];
		}
		numDisplay.GetComponent<TextMesh>().text = "01";
		while(MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(0.01f);
		}
		Jukebox = StartCoroutine(MusicMustNotStop());
		ColorParty = StartCoroutine(ColorFlash());
		for (int a = 0; a < 20; a++)
		{
			numDisplay.GetComponent<TextMesh>().text = a + 1 < 10 ? "0" + (a + 1).ToString() : (a + 1).ToString();
			for (int x = 0; x < ArrowHeads.Count(); x++)
			{
				ArrowHeads[x].material = SubmittedWalls[a].Contains(DirectionsChar[x]) ? ColorArrows[3] : ColorArrows[0];
			}
			yield return new WaitForSecondsRealtime(1.49f);
			if (Walls[a] != SubmittedWalls[a])
			{
				if (!Walls[a].ToCharArray().All(c => SubmittedWalls[a].Contains(c)))
				{
					Debug.LogFormat("[Cyan Arrows #{0}] Wall {1} has a missing wall that is crucial. The car sent itself into the abyss. Prepare for an explosion.", moduleId, (a+1).ToString());
					MusicPlayer.Stop();
					StartCoroutine(SendToTheAbyss(a));
				}
				
				else if (CorrectDirections[a] != "EMPTY" && SubmittedWalls[a].Contains(CorrectDirections[a]))
				{
					Debug.LogFormat("[Cyan Arrows #{0}] Wall {1} has a wall blocking its way. The car forcefully ram itself into the wall. Prepare for a car crash.", moduleId, (a+1).ToString());
					MusicPlayer.Stop();
					StartCoroutine(ImmediateCollision(a, CorrectDirections[a]));
					
				}
				StopCoroutine(Jukebox);
				StopCoroutine(ColorParty);
				goto TheEnd;
			}
			
			if (CorrectDirections[a] == "EMPTY" )
			{
				Debug.Log("YOU SHOULD NEVER COLLIDE");
			}
			
			else if (a < 19 && !(Walls[a].Length == 3 && Walls[a + 1].Length == 3) && SubmittedWalls[a + 1].Contains(Directions[(Array.IndexOf(Directions, CorrectDirections[a]) + 2) % 4]))
			{
				Debug.LogFormat("[Cyan Arrows #{0}] Wall {1} has a currently unseen wall blocking its way. The car forcefully ram itself into the wall. Prepare for a car crash.", moduleId, (a+2).ToString());
				MusicPlayer.Stop();
				StartCoroutine(DelayedCollision(a, Directions[(Array.IndexOf(Directions, CorrectDirections[a]) + 2) % 4]));
				StopCoroutine(Jukebox);
				StopCoroutine(ColorParty);
				goto TheEnd;
			}
			
			if (MarchNumber < 19)
			{
				MarchNumber++;
			}
			yield return new WaitForSecondsRealtime(0.01f);
		}
		MusicPlayer.Stop();
		StopCoroutine(Jukebox);
		StopCoroutine(ColorParty);
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material = ColorArrows[3];
		}
		numDisplay.GetComponent<TextMesh>().color = new Color32 (0, 255, 255, 255);
		numDisplay.GetComponent<TextMesh>().text = "";
		MusicPlayer.clip = SFX[6];
		MusicPlayer.Play();
		while(MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(0.01f);
		}
		MusicPlayer.clip = SFX[7];
		MusicPlayer.Play();
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material = ColorArrows[0];
		}
		while(MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(0.01f);
		}
		MusicPlayer.clip = SFX[11];
		MusicPlayer.Play();
		numDisplay.GetComponent<TextMesh>().text = "GG";
		while(MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(0.01f);
		}
		MusicPlayer.clip = SFX[12];
		MusicPlayer.Play();
		Module.HandlePass();
		Debug.LogFormat("[Cyan Arrows #{0}] The car has been guided safely. You are safe. :)", moduleId);
		TheEnd:
		yield break;
	}
	
	IEnumerator SendToTheAbyss(int y)
	{
		numDisplay.GetComponent<TextMesh>().color = new Color(0f, 255f/255f, 255f/255f);
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material.color = !SubmittedWalls[y].Contains(DirectionsChar[x]) ? new Color(0, 255f/255f, 255f/255f) : Color.black;
		}
		string FallDirection = Walls[y];
		if (SubmittedWalls[y].Length != 0)
		{
			for (int x = 0; x < SubmittedWalls[y].Length; x++)
			{
				FallDirection = FallDirection.Replace(SubmittedWalls[y][x].ToString(), string.Empty);
			}
		}
		ArrowHeads[Array.IndexOf(Directions, FallDirection[UnityEngine.Random.Range(0, FallDirection.Length)].ToString())].material.color = Color.white;
		MusicPlayer.clip = SFX[9];
		MusicPlayer.Play();
		while (MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(.01f);
		}
		Module.HandleStrike();
		MusicPlayer.clip = SFX[10];
		MusicPlayer.Play();
		while (MusicPlayer.isPlaying)
		{
			int[] RandomColor = {UnityEngine.Random.Range(0,256), UnityEngine.Random.Range(0,256), UnityEngine.Random.Range(0,256)};
			for (int x = 0; x < ArrowHeads.Count(); x++)
			{
				ArrowHeads[x].material.color = new Color32 (Convert.ToByte(RandomColor[0]), Convert.ToByte(RandomColor[1]), Convert.ToByte(RandomColor[2]), 255);
			}
			numDisplay.GetComponent<TextMesh>().color = new Color32 (Convert.ToByte(RandomColor[0]), Convert.ToByte(RandomColor[1]), Convert.ToByte(RandomColor[2]), 255);
			yield return new WaitForSecondsRealtime(.01f);
		}
		StartCoroutine(BounceBack());
	}
	
	IEnumerator ImmediateCollision(int y, string RedPoint)
	{
		numDisplay.GetComponent<TextMesh>().color = new Color(0f, 255f/255f, 255f/255f);
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material.color = !SubmittedWalls[y].Contains(DirectionsChar[x]) ? new Color(0, 255f/255f, 255f/255f) : Color.black;
		}
		Module.HandleStrike();
		MusicPlayer.clip = SFX[8];
		MusicPlayer.Play();
		ArrowHeads[Array.IndexOf(Directions, RedPoint)].material.color = new Color32(255, 0, 0, 255);
		while (MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(.01f);
		}	
		StartCoroutine(BounceBack());
	}
	
	IEnumerator DelayedCollision(int y, string RedPoint2)
	{
		numDisplay.GetComponent<TextMesh>().color = new Color(0f, 255f/255f, 255f/255f);
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material.color = !SubmittedWalls[y + 1].Contains(DirectionsChar[x]) ? new Color(0, 255f/255f, 255f/255f) : Color.black;
		}
		Module.HandleStrike();	
		MusicPlayer.clip = SFX[8];
		MusicPlayer.Play();
		ArrowHeads[Array.IndexOf(Directions, RedPoint2)].material.color = new Color32(255, 0, 0, 255);
		numDisplay.GetComponent<TextMesh>().text = MarchNumber + 2 < 10 ? "0" + (MarchNumber + 2).ToString() : (MarchNumber + 2).ToString();
		while (MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(.01f);
		}
		StartCoroutine(BounceBack());
	}
	
	IEnumerator BounceBack()
	{
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material = ColorArrows[3];
		}
		numDisplay.GetComponent<TextMesh>().color = new Color32 (0, 255, 255, 255);
		numDisplay.GetComponent<TextMesh>().text = "";
		MusicPlayer.clip = SFX[3];
		MusicPlayer.Play();
		while (MusicPlayer.isPlaying)
		{
			yield return new WaitForSecondsRealtime(.01f);
		}
		MusicPlayer.clip = SFX[4];
		MusicPlayer.Play();
		for (int x = 0; x < ArrowHeads.Count(); x++)
		{
			ArrowHeads[x].material = ColorArrows[0];
		}
		
		Play2.sprite = Play;
		SubmittablePhase = false;
		Interactable = true;
		NumberOfTurns = new List<int>();
		Movement = new List<string>();
		Orientation = new List<string>();
		CorrectDirections = new List<string>();
		Walls = new List<string>();
		SubmittedWalls = new List<string>();
		TempWall = "";
		TheNumber = 20;
		MarchNumber = 0;
		Generate();
	}
	
	IEnumerator MusicMustNotStop()
	{
		MusicPlayer.clip = SFX[5];
		while (true)
		{
			MusicPlayer.Play();
			while(MusicPlayer.isPlaying)
			{
				yield return new WaitForSecondsRealtime(0.01f);
			}
		}
	}
	
	IEnumerator ColorFlash()
	{
		int PlaceholderNumber = MarchNumber, LoopNumber = 0;
		float[] ColorLoop = {255f, 205f, 155f, 205f};
		while (true)
		{
			if (PlaceholderNumber == MarchNumber)
			{
				yield return new WaitForSecondsRealtime(0.1f);
			}
			numDisplay.GetComponent<TextMesh>().color = new Color(0f, ColorLoop[LoopNumber]/255f, ColorLoop[LoopNumber]/255f);
			for (int x = 0; x < ArrowHeads.Count(); x++)
			{
				ArrowHeads[x].material.color = !SubmittedWalls[MarchNumber].Contains(DirectionsChar[x]) ? new Color(0, ColorLoop[LoopNumber]/255f, ColorLoop[LoopNumber]/255f) : Color.black;
			}
			PlaceholderNumber = MarchNumber;
			LoopNumber = (LoopNumber + 1) % 4;
		}
	}
	
	static string SortString(string input)
	{
		char[] characters = input.ToArray();
		Array.Sort(characters);
		return new string(characters);
	}

    //Twitch Plays

    protected IEnumerator Moves(string input)
    {
        for (int x = 0; x < input.Length; x++)
        {
            switch (input[x].ToString().ToUpper())
            {
                case "N":
                case "U":
                    buttons[0].OnInteract();
                    break;
                case "E":
                case "R":
                    buttons[1].OnInteract();
                    break;
                case "S":
                case "D":
                    buttons[2].OnInteract();
                    break;
                case "W":
                case "L":
                    buttons[3].OnInteract();
                    break;
                default:
                    break;
            }
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To play the sequence, use the command !{0} play | To proceed to the submission part of the module, use the command !{0} proceed | To submit a string of directions on a stage, use the command !{0} submit [string] (Example: !{0} submit URDL) | This module is capable of submitting multiple stages where a space triggers the display (Example: !{0} submit URDL URDL) | Valid Directions: U/R/D/L/N/E/W/S";
    #pragma warning restore 414
	
	char[] ValidDirections = {'U', 'R', 'D', 'L', 'N', 'E', 'W', 'S'};
	
    IEnumerator ProcessTwitchCommand(string command)
    {
		string[] parameters = command.Split(' ');
        Match match = Regex.Match(command, @"^\s*(play|proceed|submit)(?=\s+|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (match.Success)
        {
            yield return null;
            switch (match.Groups[1].Value.ToLower())
            {
                case "play":
                    if (!Interactable || SubmittablePhase)
                    {
                        yield return "sendtochaterror You are not able to use the \"play\" command at this moment. Command ignored.";
                        yield break;
                    }
                    Display.OnInteract();
                    yield return new WaitForSecondsRealtime(0.1f);
                    Display.OnInteractEnded();
                    break;

                case "proceed":
                    if (!Interactable || SubmittablePhase)
                    {
                        yield return "sendtochaterror You are not able to use the \"proceed\" command at this moment. Command ignored.";
                        yield break;
                    }
                    Display.OnInteract();
                    yield return new WaitForSecondsRealtime(3f);
                    Display.OnInteractEnded();
                    break;

                case "submit":
                    if (!Interactable || !SubmittablePhase)
                    {
                        yield return "sendtochaterror You are not able to use the \"submit\" command at this moment. Command ignored.";
                        yield break;
                    }

                    if (parameters.Length < 2)
                    {
                        yield return "sendtochaterror Parameter length invalid. Command ignored.";
                        yield break;
                    }

                    for (int i = 1; i < parameters.Length; i++)
                    {
                        if (!parameters[i].ToUpper().ToCharArray().All(c => ValidDirections.Contains(c)))
                        {
                            yield return "sendtochaterror The command: '" + parameters[i] + "', contains an invalid direction. Command ignored.";
                            yield break;
                        }

                        if (parameters[i].ToUpper().GroupBy(x => x).Any(g => g.Count() > 1))
                        {
                            yield return "sendtochaterror The command: '" + parameters[i] +"', contains a duplicate direction (which I do not allow). Command ignored.";
                            yield break;
                        }

                        if (!Regex.IsMatch(parameters[i], @"^\s*$"))
                        {
							yield return null;
                            yield return Moves(parameters[i]);
                            Display.OnInteract();
                            yield return new WaitForSecondsRealtime(.2f);
                        }
                    }
                    break;


            }

            if (numDisplay.GetComponent<TextMesh>().text == "20")
            {
                yield return "strike";
                yield return "solve";
            }
        }
    }
}
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Text.RegularExpressions;
using System;

public class MazeGeneratorAgent : Agent
{
    public MazeGeneratorArea MazeGeneratorArea;
    public MazeGeneratorPlayerAgent MazeGeneratorPlayerAgent;
    public GameObject PlayerGoal;
    public GameObject CellLocationPrefab;
    public GameObject WallPrefab;

    public MazeCell[,] mazeCells;
    public float CellSize = 5.4f;
    private int distanceFromStartToEnd = 0;

    // RESEARCH DATA TO BE SAVED
    public bool CaptureData = false;
    public string CSVFilePath;
    // ONCE OFF STATIC
    public string GeneratorName;
    public string GeneratorType;
    public int StepsTrained;

    // ONCE OFF UPDATED
    private int PerfectMazesGenerated;
    private int MistakesMade;

    // CONTINUOUS
    private int MazeNumber = 0;
    private int MazeRows = 0, MazeColumns = 0;
    private string Seed = "12341234123412341234";
    private int MazeComplexity;
    private DateTime startGenerating;

    public override void InitializeAgent()
    {
        if (CaptureData)
        {
            if (!MazeGeneratorArea.PlayerFileCreated)
            {
                var headings = new List<string>()
                {
                    "GeneratorName",
                    "GeneratorType",
                    "StepsTrained",
                    "PerfectMazesGenerated",
                    "MistakesMade",
                    "MazeNumber",
                    "MazeRows",
                    "MazeColumns",
                    "Seed",
                    "MazeComplexity",
                    "TotalMillisecondsToGenerate"
                };
                DataRecorder.WriteRecordToCSV(headings, CSVFilePath);
                MazeGeneratorArea.PlayerFileCreated = true;
            }
        }
        AgentReset();
    }

    public override void CollectObservations()
    {
        startGenerating = DateTime.UtcNow;
        // Previous Player Score
        AddVectorObs(MazeGeneratorArea.score);

        // Previous Maze Size
        AddVectorObs(MazeRows);
        AddVectorObs(MazeColumns);

        // Previous Difficulty


        // Previous Seed
        AddVectorObs(int.Parse($"{Seed[0]}"));
        AddVectorObs(int.Parse($"{Seed[1]}"));
        AddVectorObs(int.Parse($"{Seed[2]}"));
        AddVectorObs(int.Parse($"{Seed[3]}"));
        AddVectorObs(int.Parse($"{Seed[4]}"));
        AddVectorObs(int.Parse($"{Seed[5]}"));
        AddVectorObs(int.Parse($"{Seed[6]}"));
        AddVectorObs(int.Parse($"{Seed[7]}"));
        AddVectorObs(int.Parse($"{Seed[8]}"));
        AddVectorObs(int.Parse($"{Seed[9]}"));
        AddVectorObs(int.Parse($"{Seed[10]}"));
        AddVectorObs(int.Parse($"{Seed[11]}"));
        AddVectorObs(int.Parse($"{Seed[12]}"));
        AddVectorObs(int.Parse($"{Seed[13]}"));
        AddVectorObs(int.Parse($"{Seed[14]}"));
        AddVectorObs(int.Parse($"{Seed[15]}"));
        AddVectorObs(int.Parse($"{Seed[16]}"));
        AddVectorObs(int.Parse($"{Seed[17]}"));
        AddVectorObs(int.Parse($"{Seed[18]}"));
        AddVectorObs(int.Parse($"{Seed[19]}"));
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        MazeNumber++;

        // Call once on demand
        MazeRows = (int)vectorAction[0] + 2;
        MazeColumns = (int)vectorAction[1] + 2;

        Seed = $"" +
            $"{(int)vectorAction[2] + 1}" +
            $"{(int)vectorAction[3] + 1}" +
            $"{(int)vectorAction[4] + 1}" +
            $"{(int)vectorAction[5] + 1}" +
            $"{(int)vectorAction[6] + 1}" +
            $"{(int)vectorAction[7] + 1}" +
            $"{(int)vectorAction[8] + 1}" +
            $"{(int)vectorAction[9] + 1}" +
            $"{(int)vectorAction[10] + 1}" +
            $"{(int)vectorAction[11] + 1}" +
            $"{(int)vectorAction[12] + 1}" +
            $"{(int)vectorAction[13] + 1}" +
            $"{(int)vectorAction[14] + 1}" +
            $"{(int)vectorAction[15] + 1}" +
            $"{(int)vectorAction[16] + 1}" +
            $"{(int)vectorAction[17] + 1}" +
            $"{(int)vectorAction[18] + 1}" +
            $"{(int)vectorAction[19] + 1}" +
            $"{(int)vectorAction[20] + 1}" +
            $"{(int)vectorAction[21] + 1}";

        Regex regex = new Regex(@"[05-9]");
        if (!Seed.Contains("1") || !Seed.Contains("2") || !Seed.Contains("3") || !Seed.Contains("4") || regex.IsMatch(Seed) || Seed.Length != 20)
        {
            Done();
            SetReward(-1f);
            MazeGeneratorPlayerAgent.mazeReady = false;
            MistakesMade++;
            return;
        }

        // initialize maze
        InitializeMaze();
        CreateMaze();
        CompleteMaze();
        CalculateDistanceFromEnd();

        MazeGeneratorArea.scoreTotal = Mathf.CeilToInt(distanceFromStartToEnd * 1.5f);
        MazeGeneratorArea.penaltyThreshold = distanceFromStartToEnd;

        MazeGeneratorPlayerAgent.mazeReady = true;

        PerfectMazesGenerated++;

        // CAPTURE DATA
        var diff = (DateTime.UtcNow - startGenerating);
        Debug.Log($"{diff.Minutes}:{diff.Seconds}:{diff.Milliseconds}");

        if (CaptureData)
        {
            var data = new List<string>()
            {
                GeneratorName,
                GeneratorType,
                StepsTrained.ToString(),
                PerfectMazesGenerated.ToString(),
                MistakesMade.ToString(),
                MazeNumber.ToString(),
                MazeRows.ToString(),
                MazeColumns.ToString(),
                $"[{Seed}]",
                MazeComplexity.ToString(), // TO DO
                diff.TotalMilliseconds.ToString()
            };
            DataRecorder.WriteRecordToCSV(data, CSVFilePath);
        }
    }


    public override void AgentReset()
    {
        // Destroy all walls
        if (mazeCells != null && mazeCells.Length > 0)
        {
            DestroyMaze();
        }
        // Clear parameters
        RequestDecision();
    }

    private void DestroyMaze()
    {
        foreach (var mazeCell in mazeCells)
        {
            Destroy(mazeCell?.rightWall);
            Destroy(mazeCell?.leftWall);
            Destroy(mazeCell?.topWall);
            Destroy(mazeCell?.bottomWall);
            Destroy(mazeCell?.cellParentAndLocation);
        }
    }

    private void InitializeMaze()
    {

        mazeCells = new MazeCell[MazeRows, MazeColumns];

        for (int r = 0; r < MazeRows; r++)
        {
            for (int c = 0; c < MazeColumns; c++)
            {
                mazeCells[r, c] = new MazeCell
                {
                    cellParentAndLocation = Instantiate(CellLocationPrefab, transform.position + new Vector3(r * CellSize, 0, c * CellSize), Quaternion.identity, transform) as GameObject
                };
                mazeCells[r, c].cellParentAndLocation.name = $"Cell [{r},{c}]";
                mazeCells[r, c].cellParentAndLocation.AddComponent<MazeCellValues>();

                if (c == 0)
                {
                    mazeCells[r, c].leftWall = Instantiate(WallPrefab, transform.position + new Vector3(r * CellSize, 0, (c * CellSize) - (CellSize / 2f)), Quaternion.identity, mazeCells[r, c].cellParentAndLocation.transform) as GameObject;
                    mazeCells[r, c].leftWall.name = "Left Wall " + r + "," + c;
                }

                mazeCells[r, c].rightWall = Instantiate(WallPrefab, transform.position + new Vector3(r * CellSize, 0, (c * CellSize) + (CellSize / 2f)), Quaternion.identity, mazeCells[r, c].cellParentAndLocation.transform) as GameObject;
                mazeCells[r, c].rightWall.name = "Right Wall " + r + "," + c;

                if (r == 0)
                {
                    mazeCells[r, c].topWall = Instantiate(WallPrefab, transform.position + new Vector3((r * CellSize) - (CellSize / 2f), 0, c * CellSize), Quaternion.identity, mazeCells[r, c].cellParentAndLocation.transform) as GameObject;
                    mazeCells[r, c].topWall.name = "Top Wall " + r + "," + c;
                    mazeCells[r, c].topWall.transform.Rotate(Vector3.up * 90f);
                }

                mazeCells[r, c].bottomWall = Instantiate(WallPrefab, transform.position + new Vector3((r * CellSize) + (CellSize / 2f), 0, c * CellSize), Quaternion.identity, mazeCells[r, c].cellParentAndLocation.transform) as GameObject;
                mazeCells[r, c].bottomWall.name = "Bottom Wall " + r + "," + c;
                mazeCells[r, c].bottomWall.transform.Rotate(Vector3.up * 90f);
            }
        }

        MazeGeneratorPlayerAgent.transform.position = mazeCells[0, 0].cellParentAndLocation.transform.position;

        var temp = Instantiate(MazeGeneratorPlayerAgent, transform.parent);
        Destroy(MazeGeneratorPlayerAgent.gameObject);
        MazeGeneratorPlayerAgent = temp;

        MazeGeneratorPlayerAgent.currentCell = mazeCells[0, 0];

        PlayerGoal.transform.position = mazeCells[MazeRows - 1, MazeColumns - 1].cellParentAndLocation.transform.position;
    }

    private void CreateMaze()
    {
        MazeAlgorithm ma = new HuntAndKillMazeAlgorithm(mazeCells, Seed);
        ma.CreateMaze();
    }

    private void CompleteMaze()
    {
        for (int r = 0; r < MazeRows; r++)
        {
            for (int c = 0; c < MazeColumns; c++)
            {
                if (c == 0 & r == 0)
                    mazeCells[r, c].startCell = true;

                if (c == MazeColumns - 1 & r == MazeRows - 1)
                    mazeCells[r, c].endCell = true;

                // LOOK DOWN
                if (r < MazeRows - 1)
                {
                    if (mazeCells[r, c].bottomWall == null && mazeCells[r + 1, c].topWall == null)
                    {
                        mazeCells[r, c].bottomCell = mazeCells[r + 1, c];
                    }
                }

                // LOOK UP
                if (r > 0)
                {
                    if (mazeCells[r, c].topWall == null && mazeCells[r - 1, c].bottomWall == null)
                    {
                        mazeCells[r, c].topCell = mazeCells[r - 1, c];
                    }
                }

                // LOOK LEFT
                if (c > 0)
                {
                    if (mazeCells[r, c].leftWall == null && mazeCells[r, c - 1].rightWall == null)
                    {
                        mazeCells[r, c].leftCell = mazeCells[r, c - 1];
                    }
                }

                // LOOK RIGHT
                if (c < MazeColumns - 1)
                {
                    if (mazeCells[r, c].rightWall == null && mazeCells[r, c + 1].leftWall == null)
                    {
                        mazeCells[r, c].rightCell = mazeCells[r, c + 1];
                    }
                }

                if (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell != null)
                {
                    mazeCells[r, c].cellType = CellType.XJunction;
                }
                else if (
                    (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell == null) ||
                    (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell == null && mazeCells[r, c].bottomCell != null) ||
                    (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell == null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell != null) ||
                    (mazeCells[r, c].leftCell == null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell != null)
                    )
                {
                    mazeCells[r, c].cellType = CellType.TJunction;
                }
                else if (
                    (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell == null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell == null) || // LEFT & TOP
                    (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell == null && mazeCells[r, c].topCell == null && mazeCells[r, c].bottomCell != null) || // LEFT & BOTTOM
                    (mazeCells[r, c].leftCell == null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell == null) || // RIGHT & TOP
                    (mazeCells[r, c].leftCell == null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell == null && mazeCells[r, c].bottomCell != null) // RIGHT & BOTTOM
                    )
                {
                    mazeCells[r, c].cellType = CellType.Corner;
                }
                else if (
                    (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell == null && mazeCells[r, c].topCell == null && mazeCells[r, c].bottomCell == null) ||
                    (mazeCells[r, c].leftCell == null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell == null && mazeCells[r, c].bottomCell == null) ||
                    (mazeCells[r, c].leftCell == null && mazeCells[r, c].rightCell == null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell == null) ||
                    (mazeCells[r, c].leftCell == null && mazeCells[r, c].rightCell == null && mazeCells[r, c].topCell == null && mazeCells[r, c].bottomCell != null)
                    )
                {
                    mazeCells[r, c].cellType = CellType.DeadEnd;
                }
                else if (
                   (mazeCells[r, c].leftCell != null && mazeCells[r, c].rightCell != null && mazeCells[r, c].topCell == null && mazeCells[r, c].bottomCell == null) ||
                   (mazeCells[r, c].leftCell == null && mazeCells[r, c].rightCell == null && mazeCells[r, c].topCell != null && mazeCells[r, c].bottomCell != null)
                   )
                {
                    mazeCells[r, c].cellType = CellType.Straight;
                }
                else
                {
                    //Debug.LogError($"COMPLETE MAZE(): Something went wrong [{r},{c}]\n" + mazeCells[r, c].ToString());
                }

                var script = mazeCells[r, c].cellParentAndLocation.GetComponent<MazeCellValues>();
                script.MazeCell = mazeCells[r, c];
                script.MazeCellTop = mazeCells[r, c].topCell;
                script.MazeCellBottom = mazeCells[r, c].bottomCell;
                script.MazeCellLeft = mazeCells[r, c].leftCell;
                script.MazeCellRight = mazeCells[r, c].rightCell;
            }
        }
    }

    private void CalculateDistanceFromEnd()
    {
        distanceFromStartToEnd = DistanceToEnd(mazeCells[0, 0], FromCell.start);
    }

    public int DistanceToEnd(MazeCell mazeCell, FromCell fromCell)
    {
        var distance = 0;

        if (mazeCell.endCell)
        {
            return ++distance;
        }

        if (mazeCell.topCell != null && fromCell != FromCell.top)
        {
            distance += DistanceToEnd(mazeCell.topCell, FromCell.bottom);
        }

        if (mazeCell.bottomCell != null && fromCell != FromCell.bottom)
        {
            distance += DistanceToEnd(mazeCell.bottomCell, FromCell.top);
        }

        if (mazeCell.leftCell != null && fromCell != FromCell.left)
        {
            distance += DistanceToEnd(mazeCell.leftCell, FromCell.right);
        }

        if (mazeCell.rightCell != null && fromCell != FromCell.right)
        {
            distance += DistanceToEnd(mazeCell.rightCell, FromCell.left);
        }

        if (distance > 0)
        {
            distance++;
        }

        return distance;
    }
}

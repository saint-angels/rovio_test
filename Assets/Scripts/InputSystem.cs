﻿using Assets.Scripts.Presentation.Entities;
using Assets.Scripts.Presentation.Levels;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class InputSystem : MonoBehaviour
    {
        public event Action<EntityComponent> OnCharacterClicked = (character) => { };
        public event Action<Vector2Int> OnEmptyTileClicked = (coordinates) => { };
        public event Action OnOutOfBoundsClick = () => { };

        private LevelService levelService;

        public void Init(LevelService levelService)
        {
            this.levelService = levelService;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int clickedCoordinates = LevelGrid.MouseToGridCoordinates();
                //print("Clicked on " + clickedCoordinates);
                
                bool isPointOnLevelGrid = levelService.IsPointOnLevelGrid(clickedCoordinates.x, clickedCoordinates.y);
                if (isPointOnLevelGrid)
                {
                    EntityComponent clickedEntity = levelService.GetEntityAtPosition(clickedCoordinates.x, clickedCoordinates.y);
                    if (clickedEntity != null)
                    {
                        if (clickedEntity.Type == EntityType.Character)
                        {
                            OnCharacterClicked(clickedEntity);
                        }
                        else
                        {
                            print("Non-character entity click");
                        }
                    }
                    else
                    {
                        OnEmptyTileClicked(clickedCoordinates);
                    }
                }
                else
                {
                    OnOutOfBoundsClick();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace ElementalIndustry;

/// <summary>Recipe selection, capacity, optional run button and last result for one machine window.</summary>
internal sealed class ProcessControlPanel
{
    private readonly MachineDefinition _definition;
    private readonly RecipeDefinition[] _recipes;
    private readonly Dictionary<string, MachineryRadioButton> _buttons = new(StringComparer.Ordinal);
    private readonly MachineryText _capacity;
    private readonly MachineryText _result;
    private readonly Action? _run;
    private GameItem? _machine;

    internal PixelElement Root { get; }

    internal ProcessControlPanel(MachineDefinition definition, Action? run = null)
    {
        _definition = definition;
        _recipes = ContentDatabase.RecipesFor(definition.id).ToArray();
        _run = run;
        _capacity = new MachineryText("", 6, (HorizontalAlignmentOptions)2, true);
        _result = new MachineryText("", 6, (HorizontalAlignmentOptions)2, true);

        var stack = new MachineryStack(true, 4);
        stack.Add(_capacity);
        stack.Add(_result);
        if (_recipes.Length > 1)
        {
            var left = new MachineryStack(true, 4);
            var right = new MachineryStack(true, 4);
            for (int i = 0; i < _recipes.Length; i++)
            {
                RecipeDefinition recipe = _recipes[i];
                var button = new MachineryRadioButton(
                    Loc.Get("recipe." + recipe.id + ".name"),
                    true,
                    enabled => SetRecipe(recipe.id, enabled));
                _buttons.Add(recipe.id, button);
                (i % 2 == 0 ? left : right).Add(button);
            }
            stack.Add(MachineryStack.Horizontal(new PixelElement[] { left, right }));
        }
        if (_run != null)
        {
            var button = new MachineryButton(Loc.Get("control.assemble"));
            button.SetSize(75);
            button.SetCallback(Run);
            stack.Add(button);
        }
        Root = stack;
    }

    internal void Bind(GameItem machine)
    {
        _machine = machine;
        Processor.NoteSelection(machine, _recipes);
        Sync();
    }

    internal void Sync()
    {
        if (_machine == null) return;
        foreach (RecipeDefinition recipe in _recipes)
        {
            if (_buttons.TryGetValue(recipe.id, out MachineryRadioButton? button))
            {
                button.SetOn(RecipeSelection.IsEnabled(_machine, recipe), false);
            }
        }
        UpdateStatus();
    }

    internal void UpdateStatus()
    {
        if (_machine == null) return;
        int enabled = _recipes.Count(recipe => RecipeSelection.IsEnabled(_machine, recipe));
        if (_definition.id == Ids.ElectrolysisCell)
        {
            int throughput = 100 + Math.Clamp(MachineryHelper.GetCurrentPerformanceBonus(_machine), -100, 6300);
            _capacity.SetText(Loc.Get("status.capacity_scaled", enabled, _recipes.Length, throughput));
        }
        else if (_run != null)
        {
            _capacity.SetText(Loc.Get("status.capacity_instant"));
        }
        else
        {
            _capacity.SetText(Loc.Get("status.capacity", enabled, _recipes.Length, MachineFactory.PendingWorkCycles(_machine)));
        }

        int batches = Processor.LastBatches(_machine);
        ProcessFailure failure = Processor.LastFailure(_machine);
        if (batches > 0)
        {
            _result.SetText(failure == ProcessFailure.None
                ? Loc.Get("status.completed", batches)
                : Loc.Get("status.completed_blocked", batches, FailureText(failure)));
        }
        else
        {
            _result.SetText(FailureText(failure));
        }
    }

    private void SetRecipe(string recipeId, bool enabled)
    {
        if (_machine == null) return;
        RecipeSelection.SetEnabled(_machine, recipeId, enabled);
        Processor.NoteSelection(_machine, _recipes);
        UpdateStatus();
    }

    private void Run()
    {
        if (_machine == null || _run == null) return;
        _run();
        UpdateStatus();
    }

    private string FailureText(ProcessFailure failure) => failure switch
    {
        ProcessFailure.NoRecipesEnabled => Loc.Get("status.no_recipes"),
        ProcessFailure.NoCapacity => Loc.Get("status.no_capacity"),
        ProcessFailure.MissingInputs => Loc.Get("status.missing_inputs"),
        ProcessFailure.MissingWater => Loc.Get("status.missing_water"),
        ProcessFailure.MissingPower => Loc.Get("status.missing_power"),
        ProcessFailure.OutputSpace => Loc.Get("status.output_space"),
        ProcessFailure.OutputUnavailable => Loc.Get("status.output_unavailable"),
        _ => Loc.Get(_run != null ? "status.idle_ready" : "status.idle"),
    };
}

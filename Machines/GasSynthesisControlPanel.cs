using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

namespace ElementalIndustry;

/// <summary>Native finisher-style target selection and a manual run button.</summary>
internal sealed class GasSynthesisControlPanel
{
    private readonly RecipeDefinition[] _recipes;
    private readonly MachineryText _target;
    private readonly MachineryText _result;
    private readonly Action _run;
    private GameItem? _machine;

    internal PixelElement Root { get; }

    internal GasSynthesisControlPanel(MachineDefinition definition, Action run)
    {
        _recipes = ContentDatabase.RecipesFor(definition.id).ToArray();
        _run = run;
        var configure = new MachineryButton(Loc.Get("control.configure"));
        var output = new MachineryButton(Loc.Get("control.run"));
        _target = new MachineryText("", 6, HorizontalAlignmentOptions.Center, true);
        _result = new MachineryText("", 6, HorizontalAlignmentOptions.Center, true);
        configure.SetSize(75);
        configure.SetCallback(Configure);
        output.SetCallback(Run);
        Root = MachineryStack.Vertical(configure, _target, _result, output);
    }

    internal void Bind(GameItem machine)
    {
        _machine = machine;
        Sync();
    }

    internal void Sync()
    {
        if (_machine == null) return;
        string selected = RecipeSelection.Selected(_machine, _recipes);
        _target.SetText(Loc.Get("recipe." + selected + ".name"));
        int batches = Processor.LastBatches(_machine);
        ProcessFailure failure = Processor.LastFailure(_machine);
        _result.SetText(batches > 0
            ? Loc.Get(failure == ProcessFailure.None ? "status.completed" : "status.completed_blocked", batches, FailureText(failure))
            : FailureText(failure));
    }

    private void Configure()
    {
        if (_machine == null) return;
        CustomUIBuilder? window = CustomUIManager.Window("elem_gas_synthesis_recipes", Loc.Get("control.configure_title"));
        if (window == null) return;
        window.SetSize(520, 560).SetCloseOnEscape().SetDraggable(true);
        string selected = RecipeSelection.Selected(_machine, _recipes);
        window.BeginScroll(460);
        foreach (RecipeDefinition recipe in _recipes)
        {
            string id = recipe.id;
            window.AddHeader(Loc.Get("recipe." + id + ".name"));
            window.AddLabel(Loc.Get("control.inputs", DescribeInputs(recipe)));
            window.AddLabel(Loc.Get("control.outputs", DescribeOutputs(recipe)));
            if (recipe.water > 0)
                window.AddLabel(Loc.Get("control.water", recipe.water / 1000000.0));
            window.AddLabel(Loc.Get("control.power", recipe.power));
            window.AddButton(Loc.Get(id == selected ? "control.selected" : "control.select"), () =>
            {
                if (_machine != null) RecipeSelection.Select(_machine, id);
                CustomUIManager.Instance.CloseWindow("elem_gas_synthesis_recipes");
                Sync();
            });
            window.AddSeparator(null, 6);
        }
        window.End();
        window.Center().Show();
    }

    private static string DescribeInputs(RecipeDefinition recipe)
        => recipe.inputs.Length == 0 ? Loc.Get("control.none")
            : string.Join(" + ", recipe.inputs.Select(input => DescribeAmount(input.id, input.tag, input.count)));

    private static string DescribeOutputs(RecipeDefinition recipe)
        => recipe.outputs.Length == 0 ? Loc.Get("control.none")
            : string.Join(" + ", recipe.outputs.Select(output =>
                DescribeAmount(output.id, "", output.count)));

    private static string DescribeAmount(string id, string tag, int count)
    {
        string label = id;
        if (!string.IsNullOrEmpty(id))
        {
            string key = "item." + id + ".name";
            string localized = Loc.Get(key);
            if (localized != key) label = localized;
        }
        else if (!string.IsNullOrEmpty(tag))
        {
            label = tag;
        }
        bool gas = !string.IsNullOrEmpty(id) && ContentDatabase.Items.TryGetValue(id, out ItemDefinition? item) &&
            !string.IsNullOrEmpty(item.gas);
        return Loc.Get(gas ? "control.gas_amount" : "control.item_amount", label, count);
    }

    private void Run()
    {
        if (_machine == null) return;
        _run();
        Sync();
    }

    private static string FailureText(ProcessFailure failure) => failure switch
    {
        ProcessFailure.NoCapacity => Loc.Get("status.no_capacity"),
        ProcessFailure.MissingInputs => Loc.Get("status.missing_inputs"),
        ProcessFailure.MissingWater => Loc.Get("status.missing_water"),
        ProcessFailure.MissingPower => Loc.Get("status.missing_power"),
        ProcessFailure.OutputSpace => Loc.Get("status.output_space"),
        ProcessFailure.OutputUnavailable => Loc.Get("status.output_unavailable"),
        _ => Loc.Get("status.idle_manual"),
    };
}

﻿using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using DynamicData.Kernel;
using FluentAssertions;
using NexusMods.Abstractions.Activities;
using NexusMods.App.UI.Controls.Spine.Buttons.Download;

namespace NexusMods.UI.Tests.Controls.Spine;

public class DownloadButtonViewTests : AViewTest<SpineDownloadButtonView, SpineDownloadButtonDesignerViewModel, ISpineDownloadButtonViewModel>
{
    public DownloadButtonViewTests(IServiceProvider provider) : base(provider) { }

    [Fact]
    [Trait("FlakeyTest", "True")]
    public async Task SettingButtonToActiveAppliesProperClass()
    {
        var button = await Host.GetViewControl<Button>("ParentButton");

        await OnUi(() =>
        {
            button.Classes.Should().NotContain("Active");
        });

        ViewModel.IsActive = true;

        await EventuallyOnUi(() =>
        {
            button.Classes.Should().Contain("Active");
        });
    }

    [Fact]
    [Trait("FlakeyTest", "True")]

    public async Task SettingProgressSetsClassesAndValues()
    {
        var button = await Host.GetViewControl<Button>("ParentButton");
        var arc = await Host.GetViewControl<Arc>("ProgressArc");

        await OnUi(() =>
        {
            button.Classes.Should().Contain("Idle");
            button.Classes.Should().NotContain("Progress");
        });

        ViewModel.Progress = Percent.CreateClamped(0.5);

        await Eventually(async () =>
        {
            await OnUi(() =>
            {
                button.Classes.Should().NotContain("Idle");
                button.Classes.Should().Contain("Progress");
                arc.SweepAngle.Should().Be(180);
            });
        });


        ViewModel.Progress = Percent.CreateClamped(0.25);

        await Eventually(async () =>
        {
            await OnUi(() =>
            {
                button.Classes.Should().NotContain("Idle");
                button.Classes.Should().Contain("Progress");
                arc.SweepAngle.Should().Be(90);
            });
        });

        ViewModel.Progress = Optional<Percent>.None;
    }

    [Fact]
    [Trait("FlakeyTest", "True")]

    public async Task SettingUnitAndNumberChangesTextValues()
    {
        var numberBlock = await Host.GetViewControl<TextBlock>("NumberTextBlock");
        var unitsBlock = await Host.GetViewControl<TextBlock>("UnitsTextBlock");

        ViewModel.Number = 4.2f;
        ViewModel.Units = "foos";

        await EventuallyOnUi(() =>
        {
            numberBlock.Text.Should().Be("4.20");
            unitsBlock.Text.Should().Be("FOOS");
        });

        ViewModel.Number = 0.0f;

        await EventuallyOnUi(() =>
        {
            numberBlock.Text.Should().Be("0.00");
            unitsBlock.Text.Should().Be("FOOS");
        });

        ViewModel.Number = 0.0001f;

        await EventuallyOnUi(() =>
        {
            numberBlock.Text.Should().Be("0.00");
            unitsBlock.Text.Should().Be("FOOS");
        });

        ViewModel.Number = 1000.0f;

        await EventuallyOnUi(() =>
        {
            numberBlock.Text.Should().Be("1000.00");
            unitsBlock.Text.Should().Be("FOOS");
        });
    }

    [Fact]
    [Trait("FlakeyTest", "True")]

    public async Task ClickingTheButtonTriggersTheCommand()
    {
        await ButtonShouldFireReactiveCommand(vm => vm.Click, "ParentButton");
    }
}

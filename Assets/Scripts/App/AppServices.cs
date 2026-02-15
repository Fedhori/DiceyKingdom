/// <summary>
/// Aggregates app-scope services so features can access shared runtime systems through one container.
/// </summary>
public sealed class AppServices
{
    public UIService UI { get; }
    public AudioService Audio { get; }
    public BgmService Bgm { get; }
    public InputService Input { get; }
    public GameSpeedService GameSpeed { get; }
    public ParticleService Particle { get; }
    public SaveRuntimeService Save { get; }
    public StaticDataService StaticData { get; }
    public DevCommandService DevCommand { get; }

    public AppServices(
        UIService ui,
        AudioService audio,
        BgmService bgm,
        InputService input,
        GameSpeedService gameSpeed,
        ParticleService particle,
        SaveRuntimeService save,
        StaticDataService staticData,
        DevCommandService devCommand)
    {
        UI = ui;
        Audio = audio;
        Bgm = bgm;
        Input = input;
        GameSpeed = gameSpeed;
        Particle = particle;
        Save = save;
        StaticData = staticData;
        DevCommand = devCommand;
    }
}



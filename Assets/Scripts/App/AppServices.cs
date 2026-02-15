public sealed class AppServices
{
    public UIService UI { get; }
    public AudioManager Audio { get; }
    public BgmManager Bgm { get; }
    public InputManager Input { get; }
    public GameSpeedManager GameSpeed { get; }
    public ParticleManager Particle { get; }
    public SaveManager Save { get; }
    public StaticDataManager StaticData { get; }
    public DevCommandManager DevCommand { get; }

    public AppServices(
        UIService ui,
        AudioManager audio,
        BgmManager bgm,
        InputManager input,
        GameSpeedManager gameSpeed,
        ParticleManager particle,
        SaveManager save,
        StaticDataManager staticData,
        DevCommandManager devCommand)
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

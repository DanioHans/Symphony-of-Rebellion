using UnityEngine;

public class TaikoSceneConfig : MonoBehaviour {
    public MinigameController controller;
    public TaikoMinigame taiko;
    public TaikoChart easyChart, mediumChart, hardChart;

    void Start() {
        var diff = TaikoLaunch.I ? TaikoLaunch.I.difficulty : TaikoDifficulty.Easy;
        taiko.chart = diff switch {
            TaikoDifficulty.Easy   => easyChart ? easyChart : taiko.chart,
            TaikoDifficulty.Medium => mediumChart ? mediumChart : (hardChart ? hardChart : taiko.chart),
            _                      => hardChart ? hardChart : taiko.chart
        };
    }
}

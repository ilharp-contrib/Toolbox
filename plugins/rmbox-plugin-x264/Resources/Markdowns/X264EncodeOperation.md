# CPU 压制

使用 CPU 进行 H.264（AVC）视频压制。

## 介绍

这项操作使用 CPU 进行 H.264（AVC）视频压制，以达到最高质量压制。

如果需要 X265 压制，请使用「X265 压制」操作。

如果需要带有显卡加速的压制，请使用「显卡压制」操作。

## 使用

请参阅「关于」选项卡中的视频教程。

## 核心

使用 `x264` 二进制文件进行压制。默认使用 VideoLAN 提供的官方 `x264` 版本。你也可以使用下方推荐的第三方 `x264` 版本。

- [x264 7mod](https://maruko.appinn.me/7mod.html)（推荐）——目前最好用的 `x264` 版本，在极大地优化了性能的同时添加了 [7mod 专属参数](https://maruko.appinn.me/7mod_feature.html)。

- [down.7086.in](https://down.7086.in/)——该网站提供了 `x264` Yuuki 版本。
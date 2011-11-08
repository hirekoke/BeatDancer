BeatDancer

  PCの再生音(ステレオミキサー)から拾える音楽に合わせて踊るアプリです。
  精度は非常に悪いです。

  !! 動作は無保証です。どうしようも無くなったらタスクマネージャから
     落として下さい!!!
  また動作確認がまだ不十分なので、動作しない、落ちた等があれば、
   実行環境と共にご連絡いただければ、対応するかもしれません。
   またソースもGitHubで公開しているので、ご自分で対応していただけると小躍りします。

  内容物:
    - BeatDancer.exe: 本体
    - images: 踊り手の画像ファイル
    - readme.txt: このファイル

    - DirectShowLib-2005.dll: C#からDirectShowをいじるためのライブラリ
        <DirectShow.NET> LGPL, http://directshownet.sourceforge.net/
    - AForge.Math.dll: FFTを呼び出すために使用するライブラリ
        <AForge> LGPL, http://code.google.com/p/aforge/

  動作環境:
    - Windows 7 で確認
    - Microsoft .NET Framework 4
    - Direct X ランタイム

  インストール:
    - 適当な場所にフォルダごと置く

  アンインストール:
    - フォルダごと消す
    - 設定ファイルは <ユーザディレクトリ>\AppData\Roaming\BeatDancer\ 
      以下にあるのでフォルダごと削除

  使い方:
    - ステレオミキサーを有効にする
      * ボリュームコントロールの「録音デバイス」の設定を開く
      * ステレオミキサーが無効な場合は右クリックメニューから
        「無効なデバイスの表示」を ON にし、更に「有効」を選択
    - BeatDancer.exe を起動し、右クリックメニューから画像などを設定

  予想される問題点
    Q. Bluetooth ヘッドフォン使ってたら全然音と合わないよ？
    A. できるだけ対応したいですが、今のところは仕様です。

    Q. 重いよ…
    A. それは私の想いです。耐えてください。

  Change log:
    - 2011/xx/xx: 公開 (動画URL)

  参考URL:
    - DirectShow.NET
        http://directshownet.sourceforge.net/
    - AForge
        http://code.google.com/p/aforge/
    - Beat Detection Algorythm
        http://www.clear.rice.edu/elec301/Projects01/beat_sync/beatalgo.html

  連絡先:
    twitter: @hirekoke
    GitHub: https://github.com/hirekoke

# Changelog

## 1.5.0

### 日本語

- 前回の会議から今回の招集までの自分の移動経路を会議の全体マップに表示するようにしました
- 会議招集ボタンを押し始めた瞬間に凹むようにしました
- リプレイの自機の色をアバターの頭部の色に合わせました
- ステージ内の敵やアイテムの数が多いときにCPU性能によっては起きていたカクつきを減らしました
- 先にロードを終えた人が全員の準備を待たずに動けることがある問題を修正しました
- 役職開示演出をリニューアルして、試合開始と同時に全員が一斉に動き始められるようにしました

### English

- The meeting overview map now shows your route from the previous meeting up to the current meeting call
- The meeting button now sinks in as soon as you start pressing it
- Your color in replays now matches your avatar's head color
- Reduced stuttering that could occur on some CPUs when there are many enemies or items in the stage
- Fixed an issue where players who finished loading early could start moving before everyone else was ready
- Revamped the role reveal sequence so everyone can start moving at the same time as soon as the match begins

## 1.4.0

### 日本語

- 一人でも人狼モードを体験できるチュートリアルを実装しました。タイトル画面の右下から始められ、村人・人狼・祈祷師・爆弾魔の4つのシナリオをボット相手に遊べます
- 密かに人狼陣営が一人も配役されない「平和村」を追加しました。詳細はREADMEかゲーム内説明書を参照してください
- ロビー設定「ノルマ（マップ総額比）」を追加しました。抽出ノルマをマップに出ている貴重品の総額に対する割合で決められます。0ならバニラの計算のままです
- ロビー設定「敵のセット数」を追加しました。弱・中・強のティアごとに敵の抽選数をレベルと無関係に決められ、-1ならバニラのままです
- 祈祷師はEMFリーダーを持っているので、霊障が発生している間は死体までの距離に応じたビープ音が鳴り、通報アイコンからも波紋が広がるようにしました。近すぎると計器が振り切れます
- リプレイ再生で、会議中に発言が無い区間を通常再生でも早送りと同じ速さで飛ばすようにしました
- リプレイを開いたときの再生速度を早送り（32倍）にしました
- 同梱のリプレイビューア（HTML）に会議のテキストチャットの表示を切り替えるスイッチを追加しました
- 同梱のリプレイビューア（HTML）にプレイヤー名を隠して識別IDだけで表示する匿名化スイッチを追加しました
- 同梱のリプレイビューア（HTML）でマップ画像の上に立体地形を表示するスイッチを追加しました
- リプレイの最後に勝利陣営と勝利条件を表示するようにしました（ゲーム内のリプレイと同梱のHTMLビューアの両方）
- 結果画面に、その試合がどう決着したかを表示するようにしました
- 試合中にEscメニューを開くと、その試合のルーム設定を見られるようにしました
- 極端に長い試合ではリプレイの位置の記録が途中で打ち切られます。打ち切られた位置はシークバーの灰色帯と再生中の掲示で分かります
- 手持ちのマップを開いたまま全体マップを開こうとすると、手持ちのマップを閉じるよう促す通知を出すようにしました
- 参加者のPCの時計がずれていると、試合中の時間もずれる問題を修正しました
- 16:9以外のモニター（ウルトラワイドなど）で、結果画面の「ロビーへ戻る」ボタンなど一部のUIが画面外に出る問題を修正しました

### English

- Added a tutorial you can play on your own. Start it from the bottom right of the title screen and play four scenarios against bots: Villager, Werewolf, Shaman, and Bomber
- Added the "peaceful village", a match where nobody at all is secretly assigned to the werewolf team. See the README or the in-game manual for details
- Added the lobby setting "Quota (share of map total)". It sets the extraction quota as a share of the total value of valuables on the map. At 0 the game keeps the vanilla quota
- Added the lobby setting "Enemy sets". It sets how many enemy sets are rolled for each tier (weak, medium, strong) regardless of level; -1 keeps the vanilla curve
- The Shaman carries an EMF reader that beeps faster near a corpse and sends ripples from the report icon. Get too close, and the meter overloads.
- Replay playback now skips message-free stretches of a meeting at the fast-forward rate, even at normal speed
- Replay now starts at the fast-forward rate (32x) when you open it
- Added a switch to the bundled replay viewer (HTML) that toggles the meeting text chat
- Added an anonymize switch to the bundled replay viewer (HTML) that hides player names and shows identifiers only
- Added a switch to the bundled replay viewer (HTML) that shows the terrain, shaded by height, on top of the map image
- The replay now shows the winning team and the win condition at the end of the match (both in-game and in the bundled HTML viewer)
- The result screen now shows how the match was decided
- Open the Esc menu during a match to see the room settings for that match
- In a long enough match, replay position tracking stops partway. The seek bar greys out the rest, and a notice appears during playback when it reaches that point
- If you open the full map while your handheld map is still out, a notice now asks you to close the handheld map
- Fixed an issue where an incorrect PC clock could cause the in-game time to be out of sync.
- Fixed some UI, such as the "Return to Lobby" button on the result screen, ending up off-screen on monitors that are not 16:9, such as ultrawide displays

## 1.3.0

### 日本語

- 陣営の全滅で勝敗が決まったとき、決め手となったプレイヤーを発表する演出を挟むようにしました
- ロビー設定「会議中の敵リスポーン係数」を追加しました。会議中に消えた敵の復活と復活間隔の短縮がどれだけ進むかを0〜100%で決められます。0%なら会議の前後で敵の状況が変わらず、100%は従来どおりです
- 会議開催カウントダウン中にも貴重品詰みによる人狼の勝利が成立するようにしました
- 残り5分のチャイムと同時に警告演出が流れるようにしました
- 議論開始時に演出を追加しました
- 会議開始時に出ていたビーコン使用回数の通知をやめ、会議冒頭のTaxmanの要約だけで伝えるようにしました
- 会議の投票パネルに残り時間バーを追加しました。左端の赤い部分は投票では削れない最後の10秒です
- 会議チャットログを同じ試合の間は消さずに持ち越すようにしました。過去の会議の発言もスクロールで遡って読め、会議の区切りは見出しとスクロールバーの目盛りで分かります。ヘッダー右端の▲▼ボタンで前後の会議へジャンプできます
- 会議チャットログ・感想戦チャットに話者フィルタを追加しました。発言者の名前・アバターをクリックするとその人の発言だけが表示され、タイトルの「フィルター中」をクリックすると解除できます
- 開票演出をリニューアルしました。得票数もより見やすくなっています
- 会議中のチュートリアルを画面左下のTaxmanの吹き出しで表示するようにしました

### English

- When one team is wiped out, the game now announces the player who decided the match
- Added the lobby setting "Enemy respawn clock during meetings". It sets how far downed enemies respawn and how far the respawn interval shortens during a meeting, from 0 to 100%. At 0% you come back to the same enemies you left; 100% works as it did before
- Werewolves can now win by valuables checkmate during the meeting countdown as well
- A warning banner now sweeps across the screen with the five-minute chime
- Added an effect when discussion begins
- Removed the beacon usage notification shown at the start of a meeting; the count is now only in Taxman's recap at the top of the meeting
- The meeting vote panel now shows a remaining-time bar. The red part at the left end is the final 10 seconds that votes cannot cut into
- The meeting chat log now carries through the whole match instead of clearing at every meeting, so you can scroll back to earlier meetings. Headings and gold marks on the scrollbar show where each meeting begins, and the ▲▼ buttons at the right of the header jump between meetings
- Added a speaker filter to the meeting chat log and post-match chat. Click a speaker's name or avatar to show only that player's messages, and click the "Filtering" title to clear it
- Reworked the vote reveal. Vote counts are easier to read, too
- Taxman now gives tutorial tips during meetings from a speech bubble at the bottom left of the screen

## 1.2.0

### 日本語

- 試合結果画面に「リプレイ再生」を追加しました。ゲーム内マップを背景にプレイヤー・敵・アイテム・死体・貴重品の損失や納品・抽出ポイントの状態を時系列で見返せます。再生・速度・シーク・軌跡追跡に対応し、会議中の発言と開票結果も再現します。リプレイはファイルへ保存でき、MODに同梱した日英対応の外部ビューアで後から再生できます
- 試合結果画面に、死亡していたプレイヤーも参加できる「感想戦チャット」と、ホスト用の「ロビーへ戻る」ボタンを追加しました。結果画面では敵を退場させ、無効試合の確認画面がチャットより手前に表示されるようにしました。自動帰還の既定時間は60秒から120秒、設定上限は300秒へ延長しました
- 会議チャットログを閉じている間の新着を赤い未読マークで知らせるようにしました。また、会議招集からTaxmanの冒頭投稿までの発言が会議ログやリプレイへ残らないよう、記録・通知の開始を議論開始時に揃えました
- 死体通報ができない間（会議開催のカウントダウン中・最終納品の直前）、右下の通報アイコンを非表示にする代わりに、バツ印とTaxmanの顔を重ねた表示へ切り替えるようにしました（アイコンが消えて「通報はどこ？」と迷わないため）
- 通信による位置のズレで、爆弾魔が近くに居続けても「爆弾を仕掛けられたかもしれない…」の警告が出ないまま爆弾を仕掛けられることがある問題を修正しました
- 会議後の分散スポーンの着地点から斜面を除外しました（転倒したままワープすると斜面で転がり続け、操作できないまま転落死する恐れがあるため）
- 人狼陣営の「貴重品を記録する」をラウンド開始時はONにしました

### English

- Added a match replay to the result screen. It shows players, enemies, items, bodies, valuable losses and deliveries, and extraction point states over the game map. You can play, change speed, seek, and follow trails, and the replay reproduces meeting chat and vote results. Save a replay to a file and watch it later in the bundled English/Japanese browser viewer
- Added post-match chat, open to players who died as well, and a host-only "Return to Lobby" button to the result screen. Enemies leave while the result screen is open, and the no-contest confirmation now sits in front of the chat panel. The default automatic return time rises from 60 to 120 seconds, and the setting now goes up to 300 seconds
- The meeting chat log now marks unread messages with a red dot while closed. Chat logging, notifications, and replay recording now begin when discussion starts after Taxman's opening post, so the log and the replay leave out the meeting countdown and intro
- While reporting a body is unavailable (during the meeting countdown and right before the final delivery), the report icon in the bottom right now shows a cross mark with Taxman's face over it instead of disappearing, so you can tell that reporting is blocked rather than gone
- Fixed network position drift letting a Bomber stay next to you and plant a bomb without the "Someone may have planted a bomb on me…" warning appearing
- Post-meeting scatter spawns no longer land on slopes (a player warped while tumbling could keep rolling down the slope and fall to their death, unable to recover)
- The werewolf team's valuable recording now starts each round switched on

## 1.1.0

### 日本語

- 無効試合を追加しました。バグ・切断・事故で続行できなくなったとき、ホストが`VoidMatchKey`（既定F5）の長押しで試合を打ち切れます（確認画面のあと、もう一度長押しで確定）。勝者はつかず、コスメティックトークンも配布されませんが、結果画面と試合の記録は通常どおり表示されます
- 会議の後、生存者が多い時はランダムな組に分けてマップ各所へ分散スポーンさせるようにしました（固定メンバーでの巡回を防止。組分けは開票後に投票パネルの演出で発表されます）
- 分散スポーン直後に死亡が起きた場合、カウントダウン無しで即座に会議が始まるようにしました（この間は画面上部の残り時間の下に「引き継ぎ期間中…」と表示されます）
- 会議チャットログに「Taxman」のシステムメッセージを追加しました。ここまでの経過と前回の組分けを、ログ上部の固定枠ではなくチャットの流れの中でお知らせします
- 会議チャットに出る破壊された貴重品の額を、試合開始からの累計ではなく前回の会議からの増減に変更しました
- 会議チャットログの発言に新着の着信音を追加しました
- ラウンド残り5分の合図に専用のベルの音を追加しました
- ゲーム内説明書に「会議後の散開」のページを追加しました
- 全参加者へ試合ごとの識別番号（1〜N）を割り当て、役職公開画面・頭上・投票パネル・会議チャットログ・試合結果画面へ表示するようにしました（名前を知らない相手も「3番」と呼べます）
- 試合中の見た目（コスメ）変更を既定でブロックするようにしました
- 試合結果画面のアバターに、会議の投票パネルと同じ死因アイコン（死亡・処刑・回線落ち）を表示するようにしました
- 配信者向けセーフモードを追加しました。一部のパロディ表現や特徴的な効果音を汎用素材・無音へ置き換えます（設定方法はREADME「配信者向けセーフモード」参照）
- 前の試合の結果画面のロビー帰還タイマーが次の試合へ持ち越され、開始直後に全員が突然ロビーへ戻されることがある不具合を修正しました
- 改造クライアントが偽の試合終了・死亡発表・フェーズ変更を全員へ送りつけ、偽の結果画面などで進行を妨害できる問題を修正しました

### English

- Added no contest. When a bug, a disconnect, or an accident makes the match impossible to continue, the host can end it by holding `VoidMatchKey` (F5 by default), then holding it again to confirm. No team wins and no one gets cosmetic tokens, but the result screen and the match log appear as usual
- When a meeting ends with enough survivors, the game now splits them into random groups and scatters them across separate parts of the map (prevents fixed patrol groups; the vote panel reveals the groups once the votes are in)
- If someone dies right after a scatter spawn, a meeting now starts with no countdown (during this window, "Handover in progress…" appears below the remaining time at the top of the screen)
- The meeting chat log now carries system messages from "Taxman". The recap of what happened so far and the previous group assignment now arrive in the chat flow instead of the pinned box at the top of the log
- The recap now reports the value of destroyed valuables as the change since the previous meeting instead of the running total for the match
- Chat messages in the meeting chat log now play a notification sound as they arrive
- The five-minute mark of the round timer now rings a dedicated bell
- Added a "Scattering After Meetings" page to the in-game manual
- Every player now gets an ID number (1-N) for the match, shown on the role reveal screen, overhead, the vote panel, the meeting chat log, and the match result screen (so you can call someone "No. 3" without knowing their name)
- By default you can no longer change your appearance (cosmetics) during a match
- The match result screen now overlays avatars with the same death-cause icons (dead / executed / disconnected) as the meeting vote panel
- Added a streamer-safe mode that replaces some parody visuals and distinctive sound effects with generic assets or silence (see "Streamer-Safe Mode" in the README for how to turn it on)
- Fixed the previous match's result-screen return timer carrying over into the next match, which could send everyone back to the lobby right after the start
- Fixed modified clients being able to broadcast fake game-over, death, and phase-change messages to everyone, which could disrupt a match with things like a fake result screen

## 1.0.0

### 日本語

- 最初のリリース

### English

- Initial release

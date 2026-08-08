# Changelog

## 0.1.0

### 日本語

- 最初のベータ公開
- 会議の後、生存者が多い時はランダムな組に分けてマップ各所へ分散スポーンさせるようにしました（固定メンバーでの巡回を防止。組分けは開票後に投票パネルの演出で発表されます）
- 分散スポーン直後に死亡が起きた場合、カウントダウン無しで即座に会議が始まるようにしました
- 会議チャットログに「Taxman」のシステムメッセージ（ここまでの経過・前回の組分け）を追加し、ログ上部に固定表示していた経過の枠は統合しました
- 全参加者へ試合ごとの識別番号（1〜N）を割り当て、役職公開画面・頭上・投票パネル・会議チャットログ・試合結果画面へ表示するようにしました（名前を知らない相手も「3番」と呼べます）
- 試合中の見た目（コスメ）変更を既定でブロックするようにしました
- 試合結果画面のアバターに、会議の投票パネルと同じ死因アイコン（死亡・処刑・回線落ち）を表示するようにしました
- 配信者向けセーフモードを追加しました。一部のパロディ表現や特徴的な効果音を汎用素材・無音へ置き換えます
- 前の試合の結果画面のロビー帰還タイマーが次の試合へ持ち越され、開始直後に全員が突然ロビーへ戻されることがある不具合を修正しました

### English

- Initial beta release
- When a meeting ends and enough survivors are left, they are now split into random groups and scatter-spawned across separate parts of the map (prevents fixed patrol groups; the group assignment is revealed in the vote panel once the votes are in)
- If someone dies right after a scatter spawn, a meeting now starts immediately, with no countdown
- The meeting chat log now carries system messages from "Taxman" (a recap of what happened so far and the previous group assignment), and the pinned recap box at the top of the log has been folded into them
- Each participant is now assigned an ID number (1-N) for the match, shown on the role reveal screen, overhead, and on the vote panel, meeting chat log, and match result screen (so you can call someone "No. 3" without knowing their name)
- Changing your appearance (cosmetics) during a match is now blocked by default
- The match result screen now overlays the same death-cause icons (dead / executed / disconnected) on avatars as the meeting vote panel
- Added a streamer-safe mode that replaces some parody visuals and distinctive sound effects with generic assets or silence
- Fixed the previous match's result-screen return timer carrying over into the next match, which could send everyone back to the lobby shortly after the start

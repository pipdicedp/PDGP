# Image credits

Photography assets kept in `wwwroot/Images/`. The citizen and official login
pages no longer use a photographic backdrop: `wwwroot/css/login-puducherry.css`
clears the image and uses a plain navy gradient instead. The assets below are
retained for other surfaces and as fallbacks.

| Surface | File | Subject | Licence |
| --- | --- | --- | --- |
| Citizen dashboard banner (`Views/Dashboard/Index.cshtml`) | `Matrimandir_Auroville.jpg` | Matrimandir, Auroville | CC0 1.0 |
| Retained, not currently displayed | `Auroville_Drone_Hero.jpg` | Auroville, aerial (drone) view | CC0 1.0 |

Neither photograph imposes **an attribution duty on the department**.

---

## Auroville_Drone_Hero.jpg  (retained — no longer used on login pages)

- **Subject:** Aerial (drone) view of Auroville, near Puducherry, India
- **Source:** Wikimedia Commons — <https://commons.wikimedia.org/wiki/File:The_drone_view_of_Auroville.jpg>
- **Author:** KalkiRaj
- **Licence:** **CC0 1.0 Universal (Public Domain Dedication)** —
  <https://creativecommons.org/publicdomain/zero/1.0/>
  → **no attribution required.**
- **Resolution:** 1904x929 JPEG — a wide 2.05:1 frame, suited to a full-bleed hero.
- **Modifications:** none; the original CC0 file is served unchanged.
- Earlier references in `wwwroot/css/login.css` are now cleared by the final
  override in `wwwroot/css/login-puducherry.css`. Not currently displayed.

---

## Matrimandir_Auroville.jpg  ← current backdrop, citizen dashboard banner

- **Subject:** Matrimandir, Auroville, near Puducherry, India
- **Source:** Wikimedia Commons — <https://commons.wikimedia.org/wiki/File:Auroville.jpg>
- **Author:** Ramya Srivastav
- **Licence:** **CC0 1.0 Universal (Public Domain Dedication)** —
  <https://creativecommons.org/publicdomain/zero/1.0/>
  → **no attribution required.** The on-page placard is offered as information
  for the visitor, not as a licence condition.
- **Modifications:** downscaled from the 5152x2896 original to 3200x1800 and
  re-encoded as JPEG (quality 88).
- Earlier login-page references in `wwwroot/css/login.css` are now cleared by
  `wwwroot/css/login-puducherry.css`. Still used by `.dashboard-banner-photo`
  in `wwwroot/css/tl_css/dashboard.css` (Views/Dashboard/Index.cshtml).

---

## Auroville_Town_Hall.jpg  (retained — earlier officer login backdrop)

- **Subject:** Auroville Town Hall, near Puducherry, India
- **Source:** Wikimedia Commons — <https://commons.wikimedia.org/wiki/File:Town_Hall_of_Auroville.jpg>
- **Author:** Kaspar Konrad
- **Licence:** **Public domain** — no attribution required.
- **Modifications:** downscaled from the 5129x3419 original to 3200x2134 and
  re-encoded as JPEG (quality 88).
- Referenced by `.tl-lx-page.tl-lx-officer .tl-lx2-shell::before` (and by the
  matching `.tl-lx2-photo` override inside the `@media (max-width: 1000px)`
  block) in `wwwroot/css/login.css`.

---

## Login_Backdrop.png  (retained — not currently referenced)

- **Source:** supplied by the operator (uploaded file).
- **Licence:** not recorded. Kept on disk only as a fallback; it is no longer
  fetched by any page. **If it is put back into service, confirm its licence
  first** — a government portal should not publish a third-party image without a
  clear licence.
- **Resolution:** 612x344 PNG (16:9). Too small for a full-screen backdrop (it
  is stretched roughly 2.2x-2.4x by `cover`), which is why it was retired.
- To restore it, point the two `url(...)` backdrop values in
  `wwwroot/css/login.css` at this file and remove the officer override.

---

## Puducherry_French_Quarter.jpg  (retained — not currently referenced)

- **Subject:** French Quarter, Puducherry, India
- **Source:** Wikimedia Commons — <https://commons.wikimedia.org/wiki/File:French_Quarter_in_Pondicherry_(9)_(37256113950).jpg>
- **Author:** Richard Mortel (Riyadh, Saudi Arabia)
- **Licence:** **CC BY 2.0** — <https://creativecommons.org/licenses/by/2.0>
  - Attribution **is required** for this photograph. It was displayed on the page
    while in use; if it is ever put back on the login page, the credit line must
    be restored too (`.tl-lx2-photocredit`, with links to the author and licence).
- **Modifications:** downscaled from the 6000x4000 original to 3200x2134 and
  re-encoded as JPEG (quality 88).
- Kept so the previous backdrop can be restored by changing two `url(...)`
  values in `wwwroot/css/login.css`.

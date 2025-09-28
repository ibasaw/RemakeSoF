net-next bcrypt 
token.jwt
mongodb /driver /core

CHECKLISTE:

1. Map laden - check (in chunks)
2. charaktere laden - check (in chunks)
3. waffen laden - check (evtl. in chunks)
4. animations laden - check (in chunks)

--

TODO:

1. aus base.npc usw. die Bounds auslesen für collision boxes(capsule?)

1. gore objects/textures/material importieren und anpassen (sowohl für maps als auch charaktere/weapons/objekts)
2. hitboxen/hitcollider/box-capsule-mesh-collider/trigger richtig anpassen (sowohl für maps als auch charaktere/weapons/objekts)
3. animationen richtig anpassen (sowohl für maps als auch charaktere/weapons/objekts)
4. hit texturen richtig anpassen (sowohl für maps als auch charaktere/weapons/objekts) (scratch,shoot usw)

1. shader korrekt verarbeiten (werden in unity als custom properties in fbx mitgeladen (sowohl für maps als auch charaktere/weapons/objekts))
2. lightning richtig aus bsp laden oder neu bauen und verarbeiten (derzeit ausgeschaltet / Entity Rendering bei bsp import machen bzw. anpassen)
3. glass/feuer light material fixen in unity nach fbx import
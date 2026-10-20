(function (root) {
  "use strict";

  const TILE = 16;
  const Agents = root.EscritorioAgents;

  class OfficeScene extends Phaser.Scene {
    constructor() {
      super("OfficeScene");
      this.agentSprites = new Map();
      this.nameTags = new Map();
      this.spawns = new Map();
      this.meetingSeats = [];
      this.loungePoints = [];
      this.collisionGrid = [];
      this.selectedSlug = null;
      this.reducedMotion = false;
      this.zoomLevels = [2, 3, 4];
      this.zoomIndex = 1;
      this._seatTaken = new Set();
    }

    init(data) {
      this.manifest = data.manifest;
      this.estado = data.estado || { agentes: [] };
      this.onSelect = data.onSelect || function () {};
      this.onReady = data.onReady || function () {};
      this.reducedMotion =
        window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
      this.zoomIndex = window.innerWidth < 720 ? 0 : 1;
    }

    preload() {
      const base = "/lib/office/assets/";
      this.load.tilemapTiledJSON("map", base + (this.manifest.map || "map/escritorio.json"));
      this.load.image("tiles", base + (this.manifest.tiles?.atlas || "tiles/office-16.png"));
      this.load.atlas(
        "agents",
        base + (this.manifest.chars?.atlas || "chars/agents.png"),
        base + (this.manifest.chars?.json || "chars/agents.json")
      );
      this.load.atlas(
        "bubbles",
        base + (this.manifest.bubbles?.atlas || "ui/bubbles.png"),
        base + (this.manifest.bubbles?.json || "ui/bubbles.json")
      );
    }

    create() {
      const map = this.make.tilemap({ key: "map" });
      // atlas cells 18×18 with 1px margin/extrude
      const tileset = map.addTilesetImage("office-16", "tiles", 16, 16, 1, 2);
      this.map = map;
      this.layerChao = map.createLayer("Chao", tileset, 0, 0);
      this.layerParedes = map.createLayer("Paredes", tileset, 0, 0);
      this.layerMoveisAbaixo = map.createLayer("Moveis-abaixo", tileset, 0, 0);
      this.layerMoveisAcima = map.createLayer("Moveis-acima", tileset, 0, 0);
      if (this.layerMoveisAcima) this.layerMoveisAcima.setDepth(10000);

      this._buildCollision(map);
      this._parseObjects(map);
      this._createAnims();
      this._spawnAgents(this.estado.agentes || []);

      const w = map.widthInPixels;
      const h = map.heightInPixels;
      this.cameras.main.setBounds(0, 0, w, h);
      this.cameras.main.setZoom(this.zoomLevels[this.zoomIndex]);
      this.cameras.main.centerOn(w / 2, h / 2);
      this.cameras.main.setRoundPixels(true);

      this._setupInput();
      this._warmLight();
      this.onReady(this);
    }

    _buildCollision(map) {
      const col = map.getLayer("Colisao");
      const w = map.width;
      const h = map.height;
      this.collisionGrid = [];
      for (let y = 0; y < h; y++) {
        const row = [];
        for (let x = 0; x < w; x++) {
          let blocked = 0;
          if (col) {
            const t = col.data[y][x];
            if (t && t.index > 0) blocked = 1;
          }
          row.push(blocked);
        }
        this.collisionGrid.push(row);
      }
      // easystar
      if (typeof EasyStar !== "undefined") {
        this.finder = new EasyStar.js();
        this.finder.setGrid(this.collisionGrid);
        this.finder.setAcceptableTiles([0]);
        this.finder.enableDiagonals = false;
      }
    }

    _parseObjects(map) {
      const og = map.getObjectLayer("Objetos");
      if (!og) return;
      for (const o of og.objects) {
        const props = {};
        (o.properties || []).forEach((p) => {
          props[p.name] = p.value;
        });
        const tx = Math.floor(o.x / TILE);
        const ty = Math.floor(o.y / TILE);
        if (o.type === "spawn" && props.slug) {
          this.spawns.set(props.slug, { x: tx, y: ty });
        } else if (o.type === "ponto-reuniao") {
          this.meetingSeats.push({ x: tx, y: ty, facing: props.facing || "up", id: o.id });
        } else if (o.type === "ponto-lounge") {
          this.loungePoints.push({ x: tx, y: ty });
        } else if (o.type === "rotulo-sala") {
          this._addRoomLabel(o, props.nome || o.name);
        } else if (o.type === "area-privada") {
          const g = this.add.graphics();
          g.lineStyle(1, 0xc9a227, 0.6);
          g.strokeRect(o.x, o.y, o.width, o.height);
          g.setDepth(5);
        }
      }
    }

    _addRoomLabel(o, nome) {
      // DOM overlay handled by boot; stash positions
      if (!this.roomLabels) this.roomLabels = [];
      this.roomLabels.push({
        nome,
        x: o.x + o.width / 2,
        y: o.y,
      });
    }

    _createAnims() {
      const dirs = ["down", "left", "right", "up"];
      for (const key of Agents.CHAR_KEYS) {
        for (let d = 0; d < 4; d++) {
          const dir = dirs[d];
          const frames = [];
          for (let f = 0; f < 4; f++) {
            const frame = key + "_" + dir + "_" + f;
            if (this.textures.get("agents").has(frame)) frames.push({ key: "agents", frame });
          }
          if (!frames.length) continue;
          if (!this.anims.exists(key + "-walk-" + dir)) {
            this.anims.create({
              key: key + "-walk-" + dir,
              frames,
              frameRate: 8,
              repeat: -1,
            });
          }
          if (!this.anims.exists(key + "-idle-" + dir)) {
            this.anims.create({
              key: key + "-idle-" + dir,
              frames: [frames[0]],
              frameRate: 1,
            });
          }
          if (!this.anims.exists(key + "-type")) {
            this.anims.create({
              key: key + "-type",
              frames: [frames[0], frames[1] || frames[0]],
              frameRate: 4,
              repeat: -1,
            });
          }
        }
      }
    }

    _spawnAgents(agentes) {
      const ordered = Agents.tabOrder(agentes);
      ordered.forEach((a, i) => {
        const slug = a.slug;
        const spawn = this.spawns.get(slug) || { x: 10 + i, y: 10 };
        const charKey = Agents.charKeyForSlug(slug, i);
        const spr = this.add.sprite(spawn.x * TILE + 8, spawn.y * TILE + 8, "agents", charKey + "_down_0");
        spr.setOrigin(0.5, 0.75);
        spr.setData("slug", slug);
        spr.setData("charKey", charKey);
        spr.setInteractive({ useHandCursor: true });
        spr.on("pointerover", () => this._hover(slug, true));
        spr.on("pointerout", () => this._hover(slug, false));
        spr.on("pointerdown", () => this.selectAgent(slug));

        const shadow = this.add.ellipse(spr.x, spr.y + 4, 12, 5, 0x000000, 0.25);
        shadow.setDepth(1);

        const bubble = this.add.image(spr.x, spr.y - 14, "bubbles", "bang");
        bubble.setVisible(false);
        bubble.setDepth(20000);

        this.agentSprites.set(slug, { spr, shadow, bubble, charKey, tile: { ...spawn }, agent: a });
        this._applyBehavior(slug, a);
      });
      this.tabList = ordered.map((a) => a.slug);
    }

    applyEstado(estado) {
      this.estado = estado;
      const agentes = estado.agentes || [];
      for (const a of agentes) {
        const entry = this.agentSprites.get(a.slug);
        if (!entry) continue;
        const prev = entry.agent?.status;
        entry.agent = a;
        if (prev !== a.status || a.desatualizado !== entry._desat) {
          this._applyBehavior(a.slug, a);
        }
        entry._desat = a.desatualizado;
      }
    }

    _applyBehavior(slug, agente) {
      const entry = this.agentSprites.get(slug);
      if (!entry) return;
      const beh = Agents.behaviorForStatus(agente.status, { desatualizado: agente.desatualizado });
      entry.beh = beh;
      entry.spr.setAlpha(beh.alpha);
      if (beh.desaturate) entry.spr.setTint(0x888888);
      else entry.spr.clearTint();

      if (entry.bubble) {
        if (beh.bubble === "bang") {
          entry.bubble.setFrame("bang").setVisible(true);
          if (!this.reducedMotion) {
            this.tweens.add({
              targets: entry.bubble,
              scale: 1.15,
              yoyo: true,
              repeat: -1,
              duration: 400,
            });
          }
        } else if (beh.bubble === "sleep") {
          entry.bubble.setFrame("sleep").setVisible(true);
        } else if (beh.bubble === "clock") {
          entry.bubble.setFrame("dots").setVisible(true);
        } else {
          entry.bubble.setVisible(false);
          this.tweens.killTweensOf(entry.bubble);
        }
      }

      let target = null;
      if (beh.target === "desk") {
        target = this.spawns.get(slug);
      } else if (beh.target === "meeting") {
        target = this._claimMeetingSeat(slug);
      } else if (beh.target === "lounge") {
        target = this.loungePoints[Math.floor(Math.random() * this.loungePoints.length)] || this.spawns.get(slug);
      }

      if (!target) return;

      if (beh.wander && !this.reducedMotion) {
        this._wanderLoop(slug);
      } else {
        this._moveTo(slug, target, () => {
          this._playIdle(entry, beh);
        });
      }
    }

    _claimMeetingSeat(slug) {
      // release previous
      for (const s of this.meetingSeats) {
        if (s.takenBy === slug) {
          s.takenBy = null;
        }
      }
      const free = this.meetingSeats.find((s) => !s.takenBy);
      if (!free) return this.meetingSeats[0];
      free.takenBy = slug;
      return free;
    }

    _moveTo(slug, tile, onArrive) {
      const entry = this.agentSprites.get(slug);
      if (!entry) return;
      const start = {
        x: Math.floor(entry.spr.x / TILE),
        y: Math.floor(entry.spr.y / TILE),
      };
      const path = Agents.findPath(this.collisionGrid, start, { x: tile.x, y: tile.y });
      if (!path || path.length < 2) {
        entry.spr.x = tile.x * TILE + 8;
        entry.spr.y = tile.y * TILE + 8;
        onArrive && onArrive();
        return;
      }
      this._followPath(entry, path, onArrive);
    }

    _followPath(entry, path, onArrive) {
      const steps = path.slice(1);
      const speed = 3 * TILE; // px/s ≈ 3 tiles/s
      const run = (i) => {
        if (i >= steps.length) {
          onArrive && onArrive();
          return;
        }
        const step = steps[i];
        const nx = step.x * TILE + 8;
        const ny = step.y * TILE + 8;
        const dx = nx - entry.spr.x;
        const dy = ny - entry.spr.y;
        const dir = Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? "right" : "left") : dy > 0 ? "down" : "up";
        const anim = entry.charKey + "-walk-" + dir;
        if (this.anims.exists(anim)) entry.spr.anims.play(anim, true);
        const dist = Math.hypot(dx, dy);
        this.tweens.add({
          targets: entry.spr,
          x: nx,
          y: ny,
          duration: (dist / speed) * 1000,
          ease: "Linear",
          onComplete: () => run(i + 1),
        });
      };
      run(0);
    }

    _wanderLoop(slug) {
      const entry = this.agentSprites.get(slug);
      if (!entry || !entry.beh?.wander) return;
      const pts = this.loungePoints;
      if (!pts.length) return;
      const dest = pts[Math.floor(Math.random() * pts.length)];
      this._moveTo(slug, dest, () => {
        const pause = 3000 + Math.random() * 5000;
        this.time.delayedCall(pause, () => {
          if (entry.beh?.wander && !this.reducedMotion) this._wanderLoop(slug);
        });
      });
    }

    _playIdle(entry, beh) {
      const key = entry.charKey;
      if (beh.type && this.anims.exists(key + "-type")) {
        entry.spr.anims.play(key + "-type", true);
        if (beh.monitorGlow && !this.reducedMotion) {
          // small additive blink near sprite
          if (!entry.glow) {
            entry.glow = this.add.circle(entry.spr.x, entry.spr.y - 10, 3, 0x66ccff, 0.5);
            entry.glow.setBlendMode(Phaser.BlendModes.ADD);
            this.tweens.add({
              targets: entry.glow,
              alpha: 0.15,
              yoyo: true,
              repeat: -1,
              duration: 600,
            });
          }
          entry.glow.setPosition(entry.spr.x, entry.spr.y - 10).setVisible(true);
        }
      } else if (beh.sit) {
        entry.spr.y += 2;
        if (this.anims.exists(key + "-idle-up")) entry.spr.anims.play(key + "-idle-up", true);
      } else if (this.anims.exists(key + "-idle-down")) {
        entry.spr.anims.play(key + "-idle-down", true);
      }
      if (!beh.monitorGlow && entry.glow) entry.glow.setVisible(false);
    }

    _hover(slug, on) {
      const entry = this.agentSprites.get(slug);
      if (!entry) return;
      entry.spr.setPipeline
        ? null
        : null;
      entry.spr.setScale(on ? 1.05 : 1);
    }

    selectAgent(slug) {
      this.selectedSlug = slug;
      const entry = this.agentSprites.get(slug);
      if (!entry) return;
      this.cameras.main.pan(entry.spr.x, entry.spr.y, 400, "Cubic.easeOut");
      this.onSelect(entry.agent, entry);
    }

    selectByTab(delta) {
      if (!this.tabList?.length) return;
      let i = this.tabList.indexOf(this.selectedSlug);
      if (i < 0) i = 0;
      else i = (i + delta + this.tabList.length) % this.tabList.length;
      this.selectAgent(this.tabList[i]);
    }

    setZoomIndex(i) {
      this.zoomIndex = Math.max(0, Math.min(this.zoomLevels.length - 1, i));
      this.cameras.main.setZoom(this.zoomLevels[this.zoomIndex]);
    }

    bumpZoom(dir) {
      this.setZoomIndex(this.zoomIndex + dir);
    }

    centerMap() {
      const w = this.map.widthInPixels;
      const h = this.map.heightInPixels;
      this.cameras.main.centerOn(w / 2, h / 2);
    }

    _setupInput() {
      this.input.on("pointermove", (p) => {
        if (!p.isDown) return;
        this.cameras.main.scrollX -= (p.x - p.prevPosition.x) / this.cameras.main.zoom;
        this.cameras.main.scrollY -= (p.y - p.prevPosition.y) / this.cameras.main.zoom;
      });
      this.input.on("wheel", (_p, _o, _dx, dy) => {
        this.bumpZoom(dy > 0 ? -1 : 1);
      });
    }

    _warmLight() {
      const cam = this.cameras.main;
      const g = this.add.graphics();
      g.fillStyle(0xffcc88, 0.12);
      g.fillCircle(this.map.widthInPixels / 2, this.map.heightInPixels / 2, Math.max(this.map.widthInPixels, this.map.heightInPixels) * 0.45);
      g.setBlendMode(Phaser.BlendModes.MULTIPLY);
      g.setScrollFactor(1);
      g.setDepth(9000);
      this.warmOverlay = g;
    }

    update() {
      for (const [, entry] of this.agentSprites) {
        entry.spr.setDepth(entry.spr.y);
        entry.shadow.setPosition(entry.spr.x, entry.spr.y + 4);
        entry.shadow.setDepth(entry.spr.y - 1);
        if (entry.bubble.visible) {
          entry.bubble.setPosition(entry.spr.x, entry.spr.y - 16);
          entry.bubble.setDepth(entry.spr.y + 1);
        }
        if (entry.glow?.visible) {
          entry.glow.setPosition(entry.spr.x, entry.spr.y - 10);
          entry.glow.setDepth(entry.spr.y + 2);
        }
      }
      this._updateNameTags();
    }

    _updateNameTags() {
      const container = document.getElementById("escritorioNameTags");
      if (!container) return;
      const cam = this.cameras.main;
      const isMobile = window.innerWidth < 720;
      const tags = [];
      for (const [slug, entry] of this.agentSprites) {
        if (isMobile && slug !== this.selectedSlug) continue;
        const wp = entry.spr.getWorldTransformMatrix
          ? null
          : null;
        const sx = (entry.spr.x - cam.worldView.x) * cam.zoom;
        const sy = (entry.spr.y - 20 - cam.worldView.y) * cam.zoom;
        tags.push({
          id: slug,
          x: sx,
          y: sy,
          nome: entry.agent?.nome || slug,
          status: entry.agent?.status,
        });
      }
      const offsets = Agents.resolveNameTagOffsets(tags, 16);
      container.innerHTML = "";
      for (const t of tags) {
        const el = document.createElement("div");
        el.className = "escritorio-nametag" + (t.id === this.selectedSlug ? " is-selected" : "");
        el.style.left = t.x + "px";
        el.style.top = t.y + (offsets.get(t.id) || 0) + "px";
        const dot = document.createElement("span");
        dot.className = "escritorio-status-dot status-" + (t.status || "");
        el.appendChild(dot);
        el.appendChild(document.createTextNode(t.nome));
        container.appendChild(el);
      }
      // room labels
      const labels = document.getElementById("escritorioRoomLabels");
      if (labels && this.roomLabels) {
        labels.innerHTML = "";
        for (const r of this.roomLabels) {
          const el = document.createElement("div");
          el.className = "escritorio-room-label";
          el.textContent = r.nome;
          el.style.left = (r.x - cam.worldView.x) * cam.zoom + "px";
          el.style.top = (r.y - cam.worldView.y) * cam.zoom + "px";
          labels.appendChild(el);
        }
      }
    }
  }

  root.EscritorioOfficeScene = OfficeScene;
})(typeof self !== "undefined" ? self : this);

# RL Combat Arena

> Multi-agent physics-based melee combat in Unity ML-Agents with shared-policy PPO, local perception, dynamic teams, sword-and-shield mechanics, and emergent tactical behavior.

A reinforcement learning project where autonomous fighters learn melee combat using swords and shields in a physics-based Unity arena.

The same neural network controls every fighter. Agents receive only local sensory information, their own combat state, and relative ally/enemy classification. No attack sequences, defensive tactics, target selection rules, team roles, or scripted combat strategies are provided.

Instead, behavior emerges from repeated multi-agent interaction.

---

# Project Highlights

- Single shared PPO policy for all fighters
- Supports multiple agents and multiple teams
- Same policy used for allies and enemies
- Physics-based movement and collision handling
- Sword and shield controlled through animation-driven combat states
- Continuous movement and rotation
- Discrete attack and shield actions
- Three custom ray-based perception systems
- Local ally / enemy classification without team-specific observation channels
- No hard-coded tactical roles
- No explicit target selection
- No navigation mesh
- Reward-driven melee strategy development
- Emergent blocking, counter-attacking and precision hit behavior
- Emergent team-dependent strategies
- Reward-hacking behaviors discovered during training
- Unity ML-Agents + PPO

---

# Overview

The objective of this project was to explore how complex melee behavior can emerge when several agents share the same policy but interact through a relatively constrained physical combat system.

Each fighter can:

- move forward, backward and sideways,
- rotate freely,
- attack with a sword,
- raise or lower a shield,
- receive damage,
- block attacks from the front,
- collide physically with other fighters,
- and distinguish nearby allies from enemies.

There is no combat state machine deciding *what* an agent should do.

The environment implements only the physical rules of combat. Decisions about when to attack, defend, retreat, rotate, pursue an opponent, cooperate with an ally, or avoid engagement are left to the policy.

The arena can be configured for different layouts such as:

- `1 vs 1`
- `2 vs 1 vs 1`
- `2 vs 2`
- `2 vs 2 vs 2`
- free-for-all configurations
- larger multi-team experiments

The duel is therefore only a special case of the more general multi-agent arena.

![Combat Arena overview](docs/assets/readme/01_arena_overview.gif)

---

# Demo

## Multi-agent combat

The same shared policy controls every fighter in the arena.

Agents receive different local observations and therefore develop different actions even though the underlying neural network is identical.

![Multi-agent combat](docs/assets/readme/02_multi_agent_combat.gif)

## Sword and shield interaction

Attacks and blocks are resolved through actual weapon, body and shield geometry rather than abstract attack-distance checks.

![Sword and shield combat](docs/assets/readme/03_sword_shield.gif)

---

# Environment Design

Each fighter consists of a physics-controlled root object with separate body, sword and shield components.

The combat system is intentionally separated into several independent parts:

- agent decision logic,
- physical movement,
- animation state,
- weapon collision detection,
- health,
- team membership,
- combat resolution,
- reward assignment,
- and arena management.

This makes it possible to change combat rules or reward structure without rewriting the policy interface.

The environment manager dynamically spawns fighters, assigns teams, tracks deaths, detects surviving teams, terminates rounds, and starts new episodes.

Team colors are generated independently from the learning system and are used only for visualization.

---

# Shared Policy

Every fighter uses the same Behavior and therefore the same neural network.

The policy does not learn:

> "I am the red fighter."

Instead, observations describe relationships such as:

> "this nearby fighter is an ally"

or

> "this nearby fighter is an enemy."

Absolute team identity is not required for combat decisions.

This allows the same controller to operate across different team assignments and arena configurations.

A fighter can therefore behave differently from another fighter despite using exactly the same policy because their local observations, health, orientation, nearby weapons and combat states are different.

![Shared policy diagram](docs/assets/readme/04_shared_policy.png)

---

# Observation Space

The agent uses several independent sources of local information.

## Fighter perception

A circular ray-based sensor detects nearby fighters and classifies them relative to the observing agent.

Possible semantic categories include:

- no fighter,
- ally,
- enemy.

The observation does not require a separate sensor channel for every possible team.

This allows the number of teams to change without redesigning the neural network input around fixed team identities.

## Weapon perception

A separate sensor detects nearby combat objects such as:

- active sword,
- inactive sword,
- raised shield,
- inactive shield.

The weapon sensor ignores the fighter's own weapon geometry.

This gives the policy information about immediate combat threats independently from fighter-body detection.

## Arena perception

A third ray-based sensor detects arena boundaries.

This allows fighters to react to walls and corners without receiving their global position directly.

## Internal state

The policy also receives information about its own current combat state, including:

- normalized health,
- shield raised state,
- shield transition state,
- sword active state,
- sword animation state.

This is important because melee actions are not instantaneous.

An attack has an animation and active collision interval, while raising or lowering the shield also requires time.

The policy therefore has to learn combat timing rather than simply issuing instantaneous attack or block commands.

![Sensor visualization](docs/assets/readme/05_sensors.png)

---

# Action Space

The policy combines continuous locomotion with discrete combat actions.

## Continuous actions

Action | Description
--- | ---
Movement X | Local left / right movement
Movement Z | Local forward / backward movement
Rotation Y | Fighter rotation

Movement force is applied relative to the fighter's current orientation.

Rotation is also directly controlled by the policy.

## Discrete actions

Action | Description
--- | ---
Attack | Start sword attack
Shield | Raise / maintain shield

The sword attack is animation-driven rather than an instantaneous damage command.

The shield can be held raised but requires animation transitions between inactive and defensive states.

---

# Animation-Driven Combat

Sword and shield mechanics are synchronized with Unity animation events.

During an attack, the sword transitions through several logical states.

Only the relevant portion of the motion is treated as an active strike.

Similarly, the shield has separate:

- raising,
- raised,
- lowering,
- and inactive states.

Animation events communicate these phases to the learning system and combat resolver.

This introduces an additional temporal component to the environment.

The policy must learn not only **whether** to attack or block, but also **when**.

![Attack timing](docs/assets/readme/06_attack_timing.gif)

---

# Combat Resolution

Damage is determined through collision between actual combat objects.

When an active enemy sword enters a fighter's collision geometry, the combat resolver evaluates:

1. whether the attacker belongs to an enemy team,
2. whether the defender currently has the shield raised,
3. whether the attacker is inside the shield's frontal protection angle,
4. and whether the hit should therefore be blocked or accepted.

A successful hit can:

- deal damage,
- apply physical knockback,
- trigger reward feedback.

A successful block can:

- prevent damage,
- reduce knockback,
- generate separate reward feedback.

Friendly-fire damage is ignored.

---

# Precision Multi-Hit Attacks

One unexpected behavior appeared because the environment does not artificially restrict a sword swing to exactly one collision.

A hit occurs whenever an active sword collider enters the opponent's hitbox.

Agents discovered that during a single active attack window they can sometimes deliberately:

1. enter the opponent's hitbox,
2. leave it,
3. rotate or reposition,
4. and enter the hitbox again.

This produces two legitimate collision events during the same sword motion.

The behavior requires accurate coordination between movement, body rotation, attack timing and collider geometry.

It was not explicitly rewarded or programmed.

![Precision double hit](docs/assets/readme/07_double_hit.gif)

This became one of the clearest examples of the policy exploiting the continuous physical structure of the environment rather than merely learning a discrete sequence of combat actions.

---

# Reward Design

The reward system was deliberately kept modular so that different combat incentives could be tested independently.

Reward components explored during development include:

- positive reward for dealing damage,
- negative reward for receiving damage,
- positive reward for blocking,
- optional penalty for attacking into a block,
- death penalty,
- victory reward,
- timeout / unresolved-round penalty.

All reward coefficients can be adjusted independently.

This turned out to be one of the most important parts of the project because relatively small changes produced qualitatively different strategies.

A reward function that appeared reasonable numerically could still create unexpected behavioral incentives once several agents interacted simultaneously.

---

# Reward Hacking

## Shield farming near walls

One experimental reward configuration gave a relatively strong reward for successfully blocking attacks.

Initially this produced a desirable effect: agents learned to use the shield frequently and fights became much more defensive.

After further training, however, the policy discovered a more profitable strategy.

Some fighters intentionally moved toward arena walls or corners, allowed opponents to pressure them, and repeatedly held the shield while incoming attacks generated block rewards.

The behavior was mechanically valid but strategically undesirable.

![Shield reward farming](docs/assets/readme/08_block_farming.gif)

The policy had effectively discovered that surviving inside a repeated stream of blocked attacks could be more profitable than attempting to win the fight.

Reducing the block reward changed this equilibrium.

An earlier experiment in the opposite direction had already demonstrated the other extreme: when blocking was rewarded too weakly, shield usage almost disappeared.

This illustrates a central difficulty of multi-agent reward design:

> a reward large enough to teach a useful behavior can also become large enough to make that behavior the objective itself.

---

# Emergent Behaviors

Several strategies appeared during training without being explicitly programmed.

## Defensive timing

Agents learned to raise the shield during incoming attacks rather than simply holding it permanently.

In some policies, fighters also lowered the shield after the opponent's attack animation passed and immediately attempted a counter-attack.

![Defensive timing](docs/assets/readme/09_block_counter.gif)

## Body-assisted sword control

The sword animation itself has relatively limited degrees of freedom.

Agents discovered that rotating and translating the entire fighter during the attack changes the trajectory of the sword.

This allows the policy to effectively steer the strike using whole-body motion.

![Body-assisted strike](docs/assets/readme/10_body_rotation_attack.gif)

## Precision double hits

Agents learned to deliberately move the active sword collider out of an opponent's hitbox and back inside during a single attack window.

The resulting second hit requires considerably more precise timing than a normal attack.

![Double hit detail](docs/assets/readme/11_double_hit_detail.gif)

## Delayed engagement

An especially interesting temporary strategy appeared in a `2 vs 1 vs 1` configuration.

One of the two allied fighters would frequently disengage from the fight and remain away from the main combat.

The behavior appeared primarily when the agent had an ally.

Instead of immediately risking its own health, the fighter waited while the other three agents fought.

If opponents weakened or killed each other, the inactive fighter could enter the remaining fight later with full health.

![Delayed engagement](docs/assets/readme/12_delayed_engagement.gif)

The strategy did not remain dominant after further training.

Once the ally died, the waiting fighter was often left in a `1 vs 1 vs 1` situation where the final outcome was highly variable.

Nevertheless, the temporary policy is interesting because the environment contained no explicit concept of:

- waiting,
- sacrifice,
- backup,
- conserving health,
- or late engagement.

The behavior emerged only from the interaction between shared policy, team structure and terminal reward.

## Circular group motion

In experiments with four or more fighters, several reward configurations produced another recurring collective behavior.

Agents sometimes began moving around the arena in coordinated circular trajectories, producing visually recognizable group rotations.

![Circular group behavior](docs/assets/readme/13_circle_behavior.gif)

This behavior appeared independently in several training experiments.

It was not programmed as a formation and appears to be a stable multi-agent motion pattern generated by symmetric local interactions.

---

# Team-Dependent Behavior

One of the more interesting observations is that the policy can behave differently depending on whether allies are present even though agents have no explicit tactical role.

For example, the delayed-engagement strategy appeared primarily when another allied fighter existed.

This suggests that local ally/enemy observations are sufficient for the shared policy to condition its strategy on the social structure of the nearby fight.

No agent is explicitly designated as:

- attacker,
- defender,
- support,
- survivor,
- or bait.

Any role-like behavior must emerge dynamically from the current state.

![Team interaction](docs/assets/readme/14_team_behavior.gif)

---

# Policy Evolution

## Initial policy

Early agents mostly produced unstable movement, random attacks and poorly timed shield actions.

![Early training](docs/assets/readme/15_early_training.gif)

↓

## Intermediate policy

Agents began actively pursuing enemies, landing attacks and discovering basic shield usage.

Combat became persistent rather than accidental.

![Intermediate training](docs/assets/readme/16_intermediate_training.gif)

↓

## Tactical policy

Later training produced more structured behavior:

- attack timing,
- active blocking,
- counter-attacks,
- body rotation during strikes,
- precision multi-hit attacks,
- wall pressure,
- temporary disengagement strategies,
- and team-dependent decisions.

![Later training](docs/assets/readme/17_late_training.gif)

---

# Multi-Agent Reward Dynamics

The shared-policy setup makes reward design more complicated than single-agent control.

All fighters contribute experience to the same policy.

A combat mechanic can therefore create useful behavior for one trajectory while simultaneously changing the incentives of every other fighter involved.

This was especially visible in experiments involving:

- symmetric damage rewards and penalties,
- large death penalties,
- large victory rewards,
- block rewards,
- and terminal-only reward structures.

Reward balancing therefore focused not only on whether an individual event was desirable, but also on what repeated interaction pattern that event made profitable.

The project repeatedly demonstrated that optimizing the wrong dense signal can generate extremely competent behavior for the wrong objective.

---

# Training Configuration

Training uses PPO through Unity ML-Agents.

A representative configuration uses:

```yaml
behaviors:
  FighterBrain:
    trainer_type: ppo

    hyperparameters:
      batch_size: 2048
      buffer_size: 20480
      learning_rate: 3.0e-4
      beta: 5.0e-3
      epsilon: 0.2
      lambd: 0.95
      num_epoch: 3
      learning_rate_schedule: linear

    network_settings:
      normalize: true
      hidden_units: 256
      num_layers: 3

    reward_signals:
      extrinsic:
        gamma: 0.99
        strength: 1.0

    max_steps: 20000000
    time_horizon: 256
    summary_freq: 20000
```

The same policy is used by every fighter.

Training environments can change the number of fighters and teams without requiring separate networks for each team.

---

# Training Progress

TensorBoard was used to monitor cumulative reward and policy development.

Reward curves in this environment require careful interpretation.

A rising reward does not necessarily indicate better combat.

For example, one run showed rapid reward growth while the agents were gradually discovering the shield-farming strategy near arena walls.

Behavioral evaluation was therefore treated as an essential complement to scalar training metrics.

![TensorBoard training progress](docs/assets/readme/18_training_progress.png)

---

# Development Notes

A significant part of the project involved debugging the environment rather than changing the neural network.

Important issues included:

- animation events,
- attack active frames,
- shield transition states,
- trigger collision propagation,
- team filtering,
- ray sensor semantics,
- physical knockback,
- round termination,
- reward balance,
- and interaction between animation and physics.

One particularly important issue occurred when Animator transitions returned to the idle state before the attack or shield-lowering animation reached its final frame.

Because final Animation Events were never executed, internal combat flags could remain permanently active.

Correcting the animation transitions restored reliable combat state synchronization.

This was a useful reminder that in reinforcement learning environments, apparently strange learned behavior can originate from extremely small inconsistencies in the simulation.

---

# Key Design Decisions

Several principles guided the final architecture:

- One policy should control every fighter.
- Team identity should remain generic rather than being hard-coded into separate sensors.
- Agents should distinguish ally from enemy through relative perception.
- Combat should be physically resolved rather than calculated from abstract attack ranges.
- Sword and shield actions should require time.
- Movement should remain available during attack animations.
- Whole-body motion should be allowed to affect weapon trajectory.
- The environment should not artificially restrict valid physical strategies.
- Reward exploits should be corrected through incentive design when possible rather than by scripting tactical behavior.
- Duel combat should remain a special case of the same general multi-agent environment.

---

# Results

The project demonstrates that a relatively small shared policy can produce recognizable melee tactics from local perception and a limited action space.

Depending on reward configuration and training stage, learned agents have demonstrated:

- active enemy pursuit,
- avoidance and disengagement,
- shield timing,
- defensive positioning,
- counter-attacks,
- whole-body strike adjustment,
- repeated hits during one active attack interval,
- wall pressure,
- opportunistic waiting,
- ally-dependent strategy changes,
- and collective multi-agent movement patterns.

The most interesting result is not any single final strategy.

Instead, it is the diversity of temporary and stable behaviors that appear while the policy searches for ways to optimize the same underlying combat environment.

![Final combat montage](docs/assets/readme/19_final_montage.gif)

---

# Technologies

- Unity 6
- Unity ML-Agents
- C#
- PPO (Proximal Policy Optimization)
- Unity Physics
- Animator / Animation Events
- Custom ray-based perception
- TensorBoard
- ONNX inference

---

# Known Issues / Limitations

- Learned behavior is strongly dependent on the reward configuration.
- The policy is specialized for the current sword, shield, animation and physics setup.
- Large changes to animation timing or collider geometry can require retraining.
- Ray-based perception is intentionally abstract and does not provide visual input.
- Agents do not currently perform explicit long-term opponent modeling.
- The same shared policy controls all fighters, so individual permanent tactical roles are not assigned.
- Local perception means agents do not receive a complete global representation of the arena.
- Some strategies can exploit reward mechanics while remaining physically valid.
- Multi-agent reward curves alone are insufficient for evaluating tactical quality.
- Training is performed entirely in simulation.

---

# Future Work

Possible extensions include:

- larger team battles,
- dynamically changing team sizes,
- additional weapon types,
- stamina mechanics,
- directional attacks,
- more complex shield geometry,
- ranged weapons,
- capture-the-flag objectives,
- dodgeball-style team environments,
- recurrent policies with combat memory,
- explicit evaluation against historical checkpoints,
- automated behavioral metrics,
- and procedural arena layouts.

---

# Visual Evaluation

Because cumulative reward does not fully describe multi-agent behavior, direct visual evaluation is an important part of this project.

Useful evaluation clips include:

- standard duel,
- `2 vs 1 vs 1`,
- `2 vs 2 vs 2`,
- successful shield timing,
- counter-attack,
- precision double-hit,
- delayed engagement,
- wall-based reward farming,
- and collective circular movement.

![Evaluation grid](docs/assets/readme/20_evaluation_grid.png)

---

# License

Released under the MIT License.

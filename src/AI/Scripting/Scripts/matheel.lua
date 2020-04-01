
Log("lua: spoustim script");

State_stand = {};
State_attack = {};
State_run = {};
State_stun = {};
State_heal = {};
State_round = {};
State_step = {};
	

--attach npc and set variables
function Init()
	npc = GetNpc();
	stunedCount = 0;
	stunTime = 4000;
	summoned = false;
	stunCooldown = 3000;
	stunRechargeTime = 10000;
	healCooldown = 0;
	healRechargeTime = 5000;

	actualState = State_stand
end

--update function
function Update(miliseconds)
	if stunCooldown > 0 then
		stunCooldown = stunCooldown - miliseconds;
	end
	if stunCooldown < 0 then
		stunCooldown = 0;
	end
	
	if healCooldown > 0 then
		healCooldown = healCooldown - miliseconds
	end
	if healCooldown < 0 then
		healCooldown = 0;
	end
	
	actualState.Execute()
end


--State Stand
State_stand["Execute"] = function()
	local status = GetStatus();
	
	if status.enemySeen > 0 then
		ToAttack();
	end
end

State_stand["Enter"] = function()
	Log("LUA: state stand... entering");
end

State_stand["Exit"] = function()
	Log("LUA: state stand... exiting");
end


--State Attack
State_attack["Execute"] = function()
	local status = GetStatus();
	
	if status.enemySeen  == 0 then
		ToStand();	
	elseif stunCooldown == 0 then
		ToStun();	
	elseif status.hp < npc.character.hp and summoned == false then
		Talk("Ty si na me troufas?! Straze, na nej!");
		SummonGuards();
		summoned = true;
	elseif status.hp < npc.character.hp / 4 then
		ToRun();
	elseif not IsMoving() then
		r = math.random(100);
		if r < 5 then
			ToRound();
		elseif r < 60 then
			ToStep();
		end
	else
		if status.hp < npc.character.hp / 2 and isValidAction("Heal") and healCooldown == 0 then
			ToHeal();
		elseif isValidAction("Fireball") then
			--Talk("--Fireball--");
			Spell("Fireball");
		end
	end
end

State_attack["Enter"] = function()
	Log("LUA: state attack... enter");
	--Talk("Jdu po Tobe!!");
end

State_attack["Exit"] = function()
	Log("LUA: state attack... exiting");
end


--State Heal
State_heal["Execute"] = function()
	Talk("--Heal--");
	Spell("Heal");
	healCooldown = healRechargeTime;
	ToAttack();
end

State_heal["Enter"] = function()
	Log("LUA: state heal... enter");
end

State_heal["Exit"] = function()
	Log("LUA: state heal... exiting");
end

--State step
State_step["Execute"] = function()
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	--Log("status: " ..status.position.X .. "," ..status.position.Y);
	
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
		
	distance = calculateDistance(x, y, enemyX, enemyY);
	
	if distance > 7 then
		dirX, dirY = enemyX + x, enemyY + y;
	else
		dirX, dirY = enemyX - x, enemyY - y;
	end
		
	--normalize
	if dirX ~= 0 then	
		dirX = dirX/math.abs(dirX);
	end
	if dirY ~= 0 then
		dirY = dirY/math.abs(dirY);
	end
	targetX, targetY = x+dirX, y+dirY;
	Log("Krok z " ..x ..", " ..y .." na: " ..targetX .. ", " .. targetY);
	GoAt(targetX, targetY);
	ToAttack();
end

State_step["Enter"] = function()
	Log("LUA: state step... enter");
end

State_step["Exit"] = function()
	Log("LUA: state step... exiting");
end

--State Stun
State_stun["Execute"] = function()
	Talk("--Stun--");
	Stun(stunTime);
	stunCooldown = stunRechargeTime;
	ToAttack();
end

State_stun["Enter"] = function()
	Log("LUA: state stun... enter");
	Talk("At je z tebe socha... ");
end

State_stun["Exit"] = function()
	Log("LUA: state stun... exiting");
end


--State Run
State_run["Execute"] = function()
	Log("LUA: state run... executing");
	Hide();
end

State_run["Enter"] = function()
	Log("LUA: state run ... entering");
	Talk("Nademnou nevyhrajes!! Sbohem troufalèe...");
end
			
State_run["Exit"] = function()
	Log("LUA: state run... exiting");
end

--State round
State_round["Execute"] = function()
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
	diffX, diffY = enemyX - x, enemyY - y;
	
	--don't go too far
	if math.abs(diffX) > 7 then
		diffX = diffX/2;
	end	
		
	if math.abs(diffY) > 7 then
		diffY = diffY/2;		
	end
	
	targetX, targetY = enemyX + diffX, enemyY + diffY;
	
	GoAt(targetX, targetY);
	ToAttack();
end

State_round["Enter"] = function()
	Log("LUA: state round ... entering");
end
			
State_round["Exit"] = function()
	Log("LUA: state round... exiting");
end


function isValidAction(actionName)
	local status = GetStatus();
	local action = GetAction(actionName);
	
	if status.mana < action.manaDrain then 
		return false;
	end
	
	return true;
	
end


function calculateDistance(x1,y1,x2,y2)
	local dx = math.abs(x1-x2);
	local dy = math.abs(y1-y2);
	return math.max(dx,dy);
end

--transit functions
function ToStand()
	actualState.Exit();
	actualState = State_stand;
	actualState.Enter();
end

function ToAttack()
	actualState.Exit();
	actualState = State_attack;
	actualState.Enter();
end

function ToStun()
	actualState.Exit();
	actualState = State_stun;
	actualState.Enter();
end

function ToRun()
	actualState.Exit();
	actualState = State_run;
	actualState.Enter();
end

function ToHeal()
	actualState.Exit();
	actualState = State_heal;
	actualState.Enter();
end

function ToRound()
	actualState.Exit();
	actualState = State_round;
	actualState.Enter();
end

function ToStep()
	actualState.Exit();
	actualState = State_step;
	actualState.Enter();
end
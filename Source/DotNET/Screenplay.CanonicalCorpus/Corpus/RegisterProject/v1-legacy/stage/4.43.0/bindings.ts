// Registers the generated Arc proxies under the names the composed Scene refers to.
// Scene resolves bindings by name; routes stay owned by Arc at runtime.
import { registerCommands, registerQueryIdentity } from '@cratis/scene.components';
import { RegisterProject } from '../Projects/Registration/RegisterProject';
import { ProjectById as __sceneQuery0 } from '../Projects/Registration/ProjectLookup';

registerCommands({RegisterProject});
registerQueryIdentity("ProjectById", "sem1:a5f617c2c41e1fc5e3467488f00f5c5fbc9e778eb7327399851d58297161297e", __sceneQuery0);

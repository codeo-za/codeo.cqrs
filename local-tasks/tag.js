const gulp = requireModule("gulp-with-help"),
  gutil = requireModule("gulp-util"),
  editXml = require("gulp-edit-xml"),
  Git = require("simple-git/promise"),
  git = new Git(),
  config = require("./config"),
  canPush = require("./modules/can-push"),
  resolveGitRemote = requireModule("resolve-git-remote"),
  gitTag = requireModule("git-tag"),
  containingFolder = `src/${config.packageProject}`;

gulp.task("tag", () => {
  return new Promise(async (resolve, reject) => {
    const pushAllowed = await canPush();
    if (!pushAllowed) {
      reject("Push not allowed to remote; refusing to tag");
    }
    gulp.src(`${containingFolder}/Package.nuspec`).pipe(
      editXml(xml => {
        const node = xml.package.metadata[0].version,
          version = node[0].trim();

        gutil.log(gutil.colors.cyan(`Tagging at: "v${version}"`));
        gitTag(`v${version}`, `:bookmark: ${version}`)
          .then(() => {
            resolve();
          })
          .catch(err => reject(err));
        return xml;
      })
    );
  });
});

gulp.task("push-tags", "Pushes tags and commits", async () => {
  await gitPushTags();
  await gitPush();
  gutil.log(gutil.colors.green("-> all commits and tags pushed!"))
});

async function gitPushTags() {
  gutil.log(gutil.colors.green("pushing tags..."));
  const remote = await resolveGitRemote();
  await git.pushTags(remote);
  return new Promise((resolve, reject) => {
    git.pushTags(remote, err => {
      if (err) {
        return reject(err);
      }
      resolve();
    });
  });
}

async function gitPush() {
  const
    remote = await resolveGitRemote(),
    branch = await resolveGitBranch();
  gutil.log(gutil.colors.green("pushing local commits..."));
  await git.push(remote, branch);
}

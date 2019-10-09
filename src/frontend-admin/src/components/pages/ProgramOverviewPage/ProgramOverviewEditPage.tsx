import React, {useState, useEffect, useContext} from 'react'
import {AxiosService} from '../../../services/AxiosService/AxiosService'
import {History} from 'history'
import {match} from 'react-router-dom'

// Components
import {DetailPageTable, ITableItem} from '../../reusables/DetailPageTable/DetailPageTable'
import {Button} from '../../reusables/Button/Button'
import {Group} from '../../reusables/Group/Group'
import DatePicker from 'react-datepicker'
import {PictureInput} from '../../reusables/PictureInput/PictureInput'
import {ProgramForm, IProgramFormInputs} from '../../reusables/ProgramForm/ProgramForm'
import {Checkbox} from '../../reusables/Checkbox/Checkbox'
import {BackButton} from '../../reusables/BackButton/BackButton'
import {Input} from '../../reusables/Input/Input'

// Utils
import {formatDate} from '../../../utilities/FormatDate'
import {format} from '../../../utilities/formatEmptyStrings'
import {concatStyles as s} from '../../../utilities/mikesConcat'
import {formatCost} from '../../../utilities/FormatCost'
import {putUploadImage} from '../../../utilities/UploadImage'

// Context
import {LoginContext, ThemeContext} from '../../App/App'

// Styles
import styles from './ProgramOverviewEditPage.module.css'

// Types
import {ExpectedPluginType, ExpectedProgramType} from './ProgramOverviewPage'
interface IProgramOverviewEditPageProps {
    history: History
    match: match<{id: string; archived: string}>
}

interface IPluginInfo {
    id: number
    name: string
    programName: string
    description: string
    recurringCost: number
    flatCost: number
    renewalDate: Date
    purchaseDate: Date
    monthsPerRenewal: number
}

// Primary Component
export const ProgramOverviewEditPage: React.SFC<IProgramOverviewEditPageProps> = props => {
    const {
        history,
        match: {
            params: {id, archived},
        },
    } = props
    const {loginContextVariables} = useContext(LoginContext)
    const {isDarkMode} = useContext(ThemeContext)

    const [programRows, setProgramRows] = useState<ITableItem[][]>([])
    const [removedProgramRows, setRemovedProgramRows] = useState<ITableItem[][]>([])

    const [pluginRows, setPluginRows] = useState<ITableItem[][]>([])
    const [removedPluginRows, setRemovedPluginRows] = useState<ITableItem[][]>([])

    const [pluginForm, setPluginForm] = useState(false)
    const [programForm, setProgramForm] = useState({edit: false, add: false})

    const programHeaders = [`${id}`, 'Employee', 'License Key', 'Renewal Date']
    const pluginHeaders = ['Plugins', 'Renewal Date', 'Cost']

    // input states
    const [imgInput, setImgInput] = useState<File>()
    const [imgLocation, setImgLocation] = useState<string>()

    const [overviewInputs, setOverviewInputs] = useState({
        name: {value: id === 'new' ? '' : id, changed: false},
        isLicense: {value: false, changed: false},
    })

    const defaultPluginInfo = {
        id: -1,
        name: '',
        programName: overviewInputs.name.value,
        description: '',
        recurringCost: 0,
        flatCost: 0,
        renewalDate: new Date(),
        purchaseDate: new Date(),
        monthsPerRenewal: 0,
    }
    const [pluginInput, setPluginInput] = useState<IPluginInfo>({...defaultPluginInfo})

    const [pluginList, setPluginList] = useState<IPluginInfo[]>([])

    // const [programInput, setProgramInput] = useState<IProgramFormInputs>()
    const [programInput, setProgramInput] = useState<IProgramFormInputs>({
        name: {value: '', changed: false},
        programName: {value: overviewInputs.name.value, changed: false},
        description: {value: '', changed: false},
        renewalDate: {value: new Date(), changed: false},
        purchaseDate: {value: new Date(), changed: false},
        purchaseLink: {value: '', changed: false},
        licenseKey: {value: '', changed: false},
        cost: {value: 0, changed: false},
        hasRecurringCost: false,
        flatCost: {value: 0, changed: false},
        hasFlatCost: false,
        monthsPerRenewal: {value: 0, changed: false},
        numCopies: {value: 1, changed: false},
    })

    const [programUpdateInput, setProgramUpdateInput] = useState<IProgramFormInputs>({
        name: {value: '', changed: false},
        programName: {value: overviewInputs.name.value, changed: false},
        description: {value: '', changed: false},
        renewalDate: {value: new Date(), changed: false},
        purchaseLink: {value: '', changed: false},
        licenseKey: {value: '', changed: false},
        cost: {value: 0, changed: false},
        hasRecurringCost: false,
        flatCost: {value: 0, changed: false},
        hasFlatCost: false,
        monthsPerRenewal: {value: 0, changed: false},
    })

    const [employeeDropdown, setEmployeeDropdown] = useState<{name: string; id: number}[]>()
    const [selectedEmployee, setSelectedEmployee] = useState<{name: string; id: number}>()

    useEffect(() => {
        var axios = new AxiosService(loginContextVariables)
        if (id !== 'new') {
            axios
                .get(`/detail/ProgramOverview/${id}/${archived === 'archived' ? true : false}`)
                .then((data: any) => {
                    setImgLocation(data[0].programOverview.icon)

                    let prog: ITableItem[][] = []
                    data[0].inDivPrograms.map((i: ExpectedProgramType) =>
                        prog.push([
                            {value: `Copy ${i.programId}`, id: i.programId, sortBy: i.programId},
                            {
                                value: format(i.employeeName),
                                id: i.employeeId,
                                sortBy: format(i.employeeName),
                            },
                            {value: format(i.programlicenseKey), sortBy: i.programlicenseKey},
                            {value: formatDate(i.renewalDate), sortBy: formatDate(i.renewalDate)},
                        ])
                    )
                    setProgramRows(prog)

                    console.log(
                        '--- data[0].programOverview\n    program = ' +
                            data[0].programOverview.program +
                            '\n    cost = ' +
                            data[0].programOverview.progCostPerYear
                    )
                    setProgramUpdateInput({
                        name: {value: data[0].programOverview.program, changed: false},
                        programName: {value: data[0].programOverview.program, changed: false},
                        // description: {value: data[0].description, changed: false},
                        description: {value: '', changed: false},
                        renewalDate: {value: new Date(), changed: false},
                        purchaseDate: {value: new Date(), changed: false},
                        // purchaseLink: {value: data[0].programPurchaseLink, changed: false},
                        purchaseLink: {value: '', changed: false},
                        licenseKey: {
                            value: data[0].programOverview.programLicenseKey
                                ? data[0].programOverview.programLicenseKey
                                : '',
                            changed: false,
                        },
                        // cost: {value: data[0].programOverview.progCostPerYear / (12 / data[0].monthsPerRenewal), changed: false},
                        cost: {value: data[0].programOverview.progCostPerYear, changed: false},
                        flatCost: {
                            value: data[0].programOverview.progFlatCost ? data[0].programOverview.progFlatCost : 0,
                            changed: false,
                        },
                        // monthsPerRenewal: {value: data[0].monthsPerRenewal, changed: false,},
                        monthsPerRenewal: {value: 0, changed: false},
                        hasFlatCost: data[0].programOverview.progFlatCost > 0 ? true : false,
                        hasRecurringCost: data[0].programOverview.progCostPerYear > 0 ? true : false,
                    })

                    setOverviewInputs(o => {
                        return {...o, isLicense: {value: data[0].programOverview.isLicense, changed: false}}
                    })

                    let plug: ITableItem[][] = []
                    let plugList: IPluginInfo[] = []
                    data[0].listOfPlugins.forEach((i: ExpectedPluginType) => {
                        plug.push([
                            {
                                value: format(i.pluginName),
                                sortBy: format(i.pluginName),
                                id: i.pluginId,
                                tooltip: i.textField,
                            },
                            {value: formatDate(i.renewalDate), sortBy: formatDate(i.renewalDate)},
                            {
                                value: formatCost(i.isCostPerYear, i.pluginCostPerYear, i.pluginFlatCost),
                                sortBy: i.pluginCostPerYear,
                            },
                        ])

                        plugList.push({
                            id: i.pluginId,
                            name: i.pluginName,
                            programName: id,
                            description: i.textField,
                            recurringCost: i.pluginCostPerYear / (12 / i.monthsPerRenewal),
                            flatCost: i.pluginFlatCost,
                            renewalDate: i.renewalDate ? new Date(i.renewalDate) : new Date(),
                            purchaseDate: i.dateBought ? new Date(i.dateBought) : new Date(),
                            monthsPerRenewal: i.monthsPerRenewal,
                        })
                    })
                    setPluginRows(plug)
                    setPluginList(plugList)
                })
                .catch((err: any) => console.error(err))

            // console.log('--Just before--')
            // axios
            //     // .get(`/detail/program/${id}`)
            //     // .get(`/detail/program/${props.match.params.id}`)
            //     .get(`/detail/program/${id}/${archived === 'archived' ? true : false}`)
            //     .then((data: any) => {
            //         console.log('----Just inside----')
            //         console.log('-------- id: ' + {id})
            //         console.log('------ data: ' + data)
            //         console.log('----- ' + props.match.params.id)
            //         setProgramInput({
            //             name: {value: data[0].programName, changed: false},
            //             programName: {value: data[0].programName, changed: false},
            //             description: {value: data[0].description, changed: false},
            //             renewalDate: {value: new Date(data[0].renewalDate), changed: false},
            //             purchaseDate: {value: new Date(data[0].dateBought), changed: false},
            //             purchaseLink: {value: data[0].programPurchaseLink, changed: false},
            //             licenseKey: {value: data[0].programLicenseKey ? data[0].programLicenseKey : '', changed: false},
            //             cost: {value: data[0].programCostPerYear / (12 / data[0].monthsPerRenewal), changed: false},
            //             flatCost: {value: data[0].programFlatCost ? data[0].programFlatCost : 0, changed: false},
            //             monthsPerRenewal: {
            //                 value: data[0].monthsPerRenewal,
            //                 changed: false,
            //             },
            //             hasFlatCost: data[0].programFlatCost > 0 ? true : false,
            //             hasRecurringCost: data[0].programCostPerYear > 0 ? true : false,
            //         })
            //         console.log('---Inside after---')
            //     })
            //     .catch((err: any) => console.error(err))
            // console.log('--Right after--')
        }

        axios
            .get(`add/hardwarePrep`)
            .then(data => {
                const employees: {name: string; id: number}[] = []
                data.map((i: {employeeName: string; employeeId: number}) =>
                    employees.push({
                        name: i.employeeName,
                        id: i.employeeId,
                    })
                )
                setEmployeeDropdown(employees)
            })
            .catch((err: any) => console.error(err))
        // }, [archived, id, loginContextVariables])
    })

    const handleProgramRemove = (row: ITableItem[]) => {
        //add to removed array
        setRemovedProgramRows([...removedProgramRows, [...row]])
    }

    const handlePluginRemove = (row: ITableItem[]) => {
        //add to removed array
        setRemovedPluginRows([...removedPluginRows, [...row]])
    }

    const handlePluginEdit = (row: ITableItem[]) => {
        //open the plugin form
        setPluginForm(true)

        var plug = pluginList.filter(plugin => plugin.id === row[0].id)
        setPluginInput({...plug[0]})
    }

    async function handleSubmit() {
        var axios = new AxiosService(loginContextVariables)
        var postProgram = {
            Program: {
                numberOfPrograms: programInput.numCopies
                    ? Number.isNaN(programInput.numCopies.value)
                        ? 1
                        : programInput.numCopies.value
                    : 1,
                ProgramName: overviewInputs.name.value,
                ProgramCostPerYear:
                    Number.isNaN(programInput.cost.value) || programInput.cost.value <= 0
                        ? 0
                        : programInput.cost.value * (12 / programInput.monthsPerRenewal.value),
                ProgramFlatCost: Number.isNaN(programInput.flatCost.value) ? 0 : programInput.flatCost.value,
                ProgramLicenseKey: programInput.licenseKey.value,
                IsLicense: overviewInputs.isLicense.value,
                Description: programInput.description.value,
                ProgramPurchaseLink: programInput.purchaseLink.value,
                DateBought: programInput.purchaseDate
                    ? programInput.purchaseDate.value.toISOString()
                    : new Date().toISOString(),
                RenewalDate:
                    programInput.monthsPerRenewal.value === 0 ? null : programInput.renewalDate.value.toISOString(),
                MonthsPerRenewal: programInput.cost.value
                    ? Number.isNaN(programInput.monthsPerRenewal.value) || programInput.monthsPerRenewal.value === 0
                        ? 1 //default is monthly
                        : programInput.monthsPerRenewal.value
                    : null,
                EmployeeId: selectedEmployee ? selectedEmployee.id : null,
            },
        }
        if (id === 'new') {
            var msg: string = ''

            if (
                postProgram.Program.numberOfPrograms >= 1 &&
                postProgram.Program.ProgramName &&
                postProgram.Program.DateBought
            ) {
                await axios
                    .post('/add/program', postProgram)
                    .then(async (response: any) => {
                        if (response.status === 201) {
                            msg = programInput.numCopies
                                ? programInput.numCopies.value +
                                  ' copies of ' +
                                  overviewInputs.name.value +
                                  ' were added to inventory!'
                                : ''
                            window.alert(msg)
                        }
                        const {newId, newName} = response.data[0]

                        // Upload the image
                        if (imgInput) {
                            const imageLocation = `/image/program/${newId}`
                            putUploadImage(imgInput, imageLocation, axios)
                        }

                        // after submitting go back to detail
                        history.push({
                            pathname: `/programs/overview/${newName}/inventory`,
                            state: {prev: history.location},
                        })
                        return
                    })
                    .catch((err: any) => console.error(err))

                //after submitting go back to detail
                history.push({
                    pathname: `/programs/overview/${postProgram.Program.ProgramName}/inventory`,
                    state: {prev: history.location},
                })
            } else {
                msg = 'Failed because: \n'
                msg += postProgram.Program.numberOfPrograms < 1 ? 'Not enough copies,\n' : ''
                msg += postProgram.Program.ProgramName === '' ? 'No name entered,\n' : ''
                window.alert(msg)
            }
        } else {
            //Add x# of new copies w/ programInput
            if (programForm.add) {
                postProgram.Program.ProgramName = id
                if (postProgram.Program.numberOfPrograms >= 1 && postProgram.Program.DateBought) {
                    await axios
                        .post('/add/program', postProgram)
                        .then((response: any) => {
                            if (response.status === 201) {
                                msg = programInput.numCopies
                                    ? programInput.numCopies.value +
                                      ' copies of ' +
                                      overviewInputs.name.value +
                                      ' were added to inventory!'
                                    : ''
                                window.alert(msg)
                            }
                            return
                        })
                        .catch((err: any) => console.error(err))

                    //after submitting go back to detail
                    history.push({
                        pathname: `/programs/overview/${id}/inventory`,
                        state: {prev: history.location},
                    })
                } else {
                    msg = 'Failed because: \n'
                    msg += postProgram.Program.numberOfPrograms < 1 ? 'Not enough copies,\n' : ''
                    msg += postProgram.Program.ProgramName === '' ? 'No name entered,\n' : ''
                    window.alert(msg)
                }
            }

            //Edit all existing copies w/ programInput
            if (programForm.edit || overviewInputs.isLicense.changed || overviewInputs.name.changed) {
                var updateProgram = {
                    Program: {
                        OldProgramName: id,
                        ProgramName: overviewInputs.name.changed ? overviewInputs.name.value : null,
                        ProgramCostPerYear: programUpdateInput.cost.changed
                            ? programUpdateInput.cost.value * (12 / programUpdateInput.monthsPerRenewal.value)
                            : null,
                        ProgramFlatCost: programUpdateInput.flatCost.changed ? programUpdateInput.flatCost.value : null,
                        ProgramLicenseKey: programUpdateInput.licenseKey.changed
                            ? programUpdateInput.licenseKey.value
                            : null,
                        IsLicense: overviewInputs.isLicense.changed ? overviewInputs.isLicense.value : null,
                        Description: programUpdateInput.description.changed
                            ? programUpdateInput.description.value
                            : null,
                        ProgramPurchaseLink: programUpdateInput.purchaseLink.changed
                            ? programUpdateInput.purchaseLink.value
                            : null,
                        RenewalDate: programUpdateInput.renewalDate.changed
                            ? programUpdateInput.renewalDate.value.toISOString()
                            : null,
                        MonthsPerRenewal: programUpdateInput.monthsPerRenewal.changed
                            ? programUpdateInput.monthsPerRenewal.value
                            : null,
                        EmployeeId: selectedEmployee ? selectedEmployee.id : null,
                    },
                }

                await axios.put(`update/programall`, updateProgram).catch((err: any) => console.error(err))

                //after submitting go back to detail
                history.push({
                    pathname: `/programs/overview/${
                        overviewInputs.name.changed ? overviewInputs.name.value : id
                    }/inventory`,
                    state: {prev: history.location},
                })
            }

            if (pluginForm) {
                setPluginInput({...pluginInput, programName: overviewInputs.name.value})
                var postPlugin = {
                    PluginId: pluginInput.id,
                    ProgramName: id,
                    PluginName: pluginInput.name,
                    PluginFlatCost: pluginInput.flatCost,
                    TextField: pluginInput.description,
                    PluginCostPerYear: pluginInput.monthsPerRenewal
                        ? pluginInput.recurringCost * (12 / pluginInput.monthsPerRenewal)
                        : 0,
                    RenewalDate: pluginInput.renewalDate.toISOString(),
                    MonthsPerRenewal: pluginInput.monthsPerRenewal ? pluginInput.monthsPerRenewal : 0,
                    DateBought: pluginInput.purchaseDate.toISOString(),
                }

                if (
                    (postPlugin.PluginCostPerYear || postPlugin.PluginFlatCost) &&
                    format(postPlugin.PluginName) !== '-'
                ) {
                    if (pluginInput.id === -1) {
                        await axios.post('/add/plugin', postPlugin).catch((err: any) => console.error(err))

                        //after submitting go back to detail
                        history.push({pathname: `/programs/overview/${id}/inventory`, state: {prev: history.location}})
                    } else {
                        await axios.put('/update/plugin', postPlugin).catch((err: any) => console.error(err))

                        //after submitting go back to detail
                        history.push({pathname: `/programs/overview/${id}/inventory`, state: {prev: history.location}})
                    }
                } else {
                    msg = 'Failed to Add Plugin Because: \n'
                    msg += format(postPlugin.PluginName) === '-' ? 'No name entered,\n' : ''
                    msg += !postPlugin.PluginCostPerYear && !postPlugin.PluginFlatCost ? 'No cost entered,\n' : ''
                    window.alert(msg)
                }
            }

            if (removedPluginRows.length > 0) {
                removedPluginRows.forEach(remove =>
                    axios
                        .put(`archive/plugin/${remove[0].id}`, {})
                        .then(() =>
                            history.push({
                                pathname: `/programs/overview/${id}/inventory`,
                                state: {prev: history.location},
                            })
                        )
                        .catch((err: any) => console.error(err))
                )
                setRemovedPluginRows([])
            }

            if (removedProgramRows.length > 0) {
                if (removedProgramRows.length === programRows.length && pluginRows.length > 0) {
                    window.alert('Please archive the plugins before you archive all copies of this program.')
                    window.location.reload()
                } else {
                    removedProgramRows.forEach(remove => {
                        axios
                            .put(`archive/programs`, [remove[0].id])
                            .then(() =>
                                history.push({
                                    pathname: `/programs/overview/${id}/inventory`,
                                    state: {prev: history.location},
                                })
                            )
                            .catch((err: any) => console.error(err))
                    })
                    setRemovedProgramRows([])
                    //after submitting go back to detail
                }
            }
            history.push({
                pathname: `/programs/overview/${id}/inventory`,
                state: {prev: history.location},
            })
        }

        if (imgInput && imgLocation) {
            //after submitting go back to detail
            const cb = () =>
                history.push({
                    pathname: `/programs/overview/${id}/inventory`,
                    state: {prev: history.location},
                })
            putUploadImage(imgInput, imgLocation, axios, cb)
        }
    }

    const displayCopies = () => {
        var arr: ITableItem[][] = []

        if (removedProgramRows.length === 0) {
            arr = [...programRows]
        } else {
            var bools: boolean[] = []
            programRows.forEach((row: ITableItem[], index: number) => {
                bools[index] = true
                removedProgramRows.forEach((remove: ITableItem[]) => {
                    bools[index] = bools[index] && remove[0].id !== row[0].id
                })
            })
            programRows.forEach((row: ITableItem[], index: number) => {
                if (bools[index]) {
                    arr.push(row)
                }
            })
        }

        return arr
    }

    const displayPlugins = () => {
        var arr: ITableItem[][] = []

        if (removedPluginRows.length === 0) {
            arr = [...pluginRows]
        } else {
            var bools: boolean[] = []
            pluginRows.forEach((row: ITableItem[], index: number) => {
                bools[index] = true
                removedPluginRows.forEach((remove: ITableItem[]) => {
                    bools[index] = bools[index] && remove[0].id !== row[0].id
                })
            })
            pluginRows.forEach((row: ITableItem[], index: number) => {
                if (bools[index]) {
                    arr.push(row)
                }
            })
        }

        return arr
    }

    return (
        <div className={s(styles.progOverviewEditMain, isDarkMode ? styles.backgroundDark : {})}>
            <div className={styles.columns}>
                {/* column 1 */}
                <div className={styles.firstColumn}>
                    <BackButton history={history} className={styles.backButton} />
                    <div className={styles.imgContainer}>
                        <PictureInput setImage={setImgInput} image={imgInput} />
                    </div>
                    <div className={styles.submitContainer}>
                        <Button text='Submit' onClick={handleSubmit} className={styles.submitbutton} />
                    </div>
                </div>
                {/* column 2 */}
                <div className={styles.secondColumn}>
                    <div className={s(styles.title, isDarkMode ? styles.titleDark : {})}>Program Information</div>

                    <Group justify={'between'} className={styles.row1Group}>
                        <div className={id !== 'new' ? styles.nameInput : styles.nameInputWithEdit}>
                            <Input
                                label={'Program Name'}
                                value={overviewInputs.name.value}
                                onChange={(e: any) =>
                                    setOverviewInputs({...overviewInputs, name: {value: e.target.value, changed: true}})
                                }
                                maxChars={100}
                                width={'100%'}
                            />
                        </div>

                        <Checkbox
                            className={styles.checkBoxContainer}
                            boxClassName={styles.checkBox}
                            checked={overviewInputs.isLicense.value}
                            title={'License'}
                            onClick={() =>
                                setOverviewInputs({
                                    ...overviewInputs,
                                    isLicense: {value: !overviewInputs.isLicense.value, changed: true},
                                })
                            }
                        />

                        {id !== 'new' && (
                            <Button
                                className={styles.editButton}
                                onClick={() => {
                                    setProgramForm({add: false, edit: !programForm.edit})
                                }}
                                text={`Edit All Copies`}
                            />
                        )}
                    </Group>
                    {programForm.edit && (
                        <div className={styles.programForm}>
                            {/* <ProgramForm state={programUpdateInput} setState={setProgramUpdateInput} /> */}
                            {programInput && (
                                <ProgramForm
                                    state={programUpdateInput}
                                    setState={setProgramUpdateInput}
                                    // state={programInput}
                                    // setState={setProgramInput}
                                    employeeDropdown={employeeDropdown}
                                    selectedEmployee={selectedEmployee}
                                    setSelectedEmployee={setSelectedEmployee}
                                />
                            )}
                        </div>
                    )}

                    {id !== 'new' ? (
                        <Group direction={'column'}>
                            <div className={styles.programTableContainer}>
                                <DetailPageTable
                                    headers={programHeaders}
                                    rows={displayCopies()}
                                    setRows={setProgramRows}
                                    edit={true}
                                    remove={handleProgramRemove}
                                    hover={false}
                                />
                            </div>

                            <Button
                                className={styles.addContainer}
                                icon='add'
                                onClick={() => {
                                    setProgramForm({edit: false, add: !programForm.add})
                                }}
                                textInside={false}
                                text={`Add Copy(s)`}
                            />
                            {programForm.add && (
                                <div className={styles.programForm}>
                                    {/* <ProgramForm state={programInput} setState={setProgramInput} /> */}
                                    {programInput && (
                                        <ProgramForm
                                            state={programInput}
                                            setState={setProgramInput}
                                            employeeDropdown={employeeDropdown}
                                            selectedEmployee={selectedEmployee}
                                            setSelectedEmployee={setSelectedEmployee}
                                        />
                                    )}
                                </div>
                            )}
                        </Group>
                    ) : (
                        <div className={styles.programForm}>
                            {/* <ProgramForm state={programInput} setState={setProgramInput} /> */}
                            {programInput && (
                                <ProgramForm
                                    state={programInput}
                                    setState={setProgramInput}
                                    employeeDropdown={employeeDropdown}
                                    selectedEmployee={selectedEmployee}
                                    setSelectedEmployee={setSelectedEmployee}
                                />
                            )}
                        </div>
                    )}

                    {id !== 'new' && (
                        <div className={styles.pluginTableContainer}>
                            <DetailPageTable
                                headers={pluginHeaders}
                                rows={displayPlugins()}
                                setRows={setPluginRows}
                                edit={true}
                                remove={handlePluginRemove}
                                editRows={handlePluginEdit}
                                hover={false}
                            />

                            <Button
                                className={styles.addContainer}
                                icon='add'
                                onClick={() => {
                                    setPluginForm(!pluginForm)
                                    setPluginInput({...defaultPluginInfo})
                                }}
                                textInside={false}
                                text={'Add Plugin'}
                            />
                        </div>
                    )}
                    {pluginForm && id !== 'new' && (
                        <div className={styles.pluginForm}>
                            <Group justify={'between'} className={styles.pluginGroup}>
                                <div className={styles.pluginInputContainerRow1}>
                                    <Input
                                        label={'Plugin Name'}
                                        value={pluginInput.name}
                                        onChange={(e: any) => setPluginInput({...pluginInput, name: e.target.value})}
                                        maxChars={100}
                                        width={'100%'}
                                    />
                                </div>

                                <div className={styles.dateInputContainer}>
                                    <div className={styles.inputText}>Purchase Date</div>
                                    <DatePicker
                                        dateFormat='MM/dd/yyyy'
                                        placeholderText={new Date().toDateString()}
                                        selected={pluginInput.purchaseDate}
                                        onChange={e => e && setPluginInput({...pluginInput, purchaseDate: e})}
                                        className={s(styles.input, styles.dateInput)}
                                    />
                                </div>

                                <div className={styles.dateInputContainer}>
                                    <div className={styles.inputText}>Renewal Date</div>
                                    <DatePicker
                                        dateFormat='MM/dd/yyyy'
                                        placeholderText={new Date().toDateString()}
                                        selected={pluginInput.renewalDate}
                                        onChange={e => e && setPluginInput({...pluginInput, renewalDate: e})}
                                        className={s(styles.input, styles.dateInput)}
                                    />
                                </div>
                            </Group>

                            <Group justify={'between'} className={styles.pluginGroup}>
                                <div className={styles.pluginInputContainerRow2}>
                                    <div className={styles.inputText}>Flat Cost</div>
                                    <input
                                        type='number'
                                        step='0.01'
                                        className={s(styles.input, styles.pluginInputRow2)}
                                        value={pluginInput.flatCost}
                                        onChange={e =>
                                            setPluginInput({...pluginInput, flatCost: parseFloat(e.target.value)})
                                        }
                                    />
                                </div>

                                <div className={styles.pluginInputContainerRow2}>
                                    <div className={styles.inputText}>Recurring Cost</div>
                                    <input
                                        type='number'
                                        step='0.01'
                                        className={s(styles.input, styles.pluginInputRow2)}
                                        value={pluginInput.recurringCost}
                                        onChange={e =>
                                            setPluginInput({
                                                ...pluginInput,
                                                recurringCost: parseFloat(e.target.value),
                                            })
                                        }
                                    />
                                </div>

                                <div className={styles.pluginInputContainerRow2}>
                                    <div className={styles.inputText}>Months Per Renewal</div>
                                    <input
                                        type='number'
                                        className={s(styles.input, styles.pluginInputRow2)}
                                        value={pluginInput.monthsPerRenewal}
                                        onChange={e =>
                                            setPluginInput({
                                                ...pluginInput,
                                                monthsPerRenewal: parseFloat(e.target.value),
                                            })
                                        }
                                    />
                                </div>
                            </Group>
                            <div className={styles.pluginGroup}>
                                <div className={styles.inputText}>Description</div>
                                <textarea
                                    className={s(styles.input, styles.description)}
                                    value={pluginInput.description}
                                    onChange={e => setPluginInput({...pluginInput, description: e.target.value})}
                                />
                            </div>
                        </div>
                    )}
                    <div className={styles.submitContainer}>
                        <Button text='Submit' onClick={handleSubmit} className={styles.submitbutton} />
                    </div>
                </div>
            </div>
        </div>
    )
}
